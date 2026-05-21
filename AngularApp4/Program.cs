using System.Text;
using System.Text.Json.Serialization;
using AngularApp4.Data;
using AngularApp4.Hubs;
using AngularApp4.Middleware;
using AngularApp4.Serialization;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.EntityFrameworkCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://*:{renderPort}");
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"];
if (string.IsNullOrWhiteSpace(mongoDatabaseName))
{
    throw new InvalidOperationException("MongoDb:DatabaseName is not configured.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMongoDB(defaultConnection, mongoDatabaseName));
builder.Services.AddScoped<DatabaseInitializer>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
builder.Services.AddScoped<IDoctorPortalEmailService, DoctorPortalEmailService>();
builder.Services.AddScoped<IInventoryAdminService, EfInventoryAdminService>();
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
builder.Services.AddScoped<IAppNotificationService, AppNotificationService>();
builder.Services.AddSingleton<IPasswordResetService, PasswordResetService>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? [];

    if (allowedOrigins.Length == 0)
    {
        allowedOrigins =
        [
            "https://localhost:44432",
            "http://localhost:4200",
            "https://nexus-hospital.vercel.app",
            "https://nexus-frontend-theta-ochre.vercel.app",
            "https://hmskathmandu.vercel.app",
            "https://nexusfrontent.vercel.app"
        ];
    }

    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexibleTimeSpanJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableFlexibleTimeSpanJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/care-communication"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddSignalR();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));
    options.AddPolicy("UserOnly", p => p.RequireRole("User"));
    options.AddPolicy("DoctorOnly", p => p.RequireRole("Doctor"));
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hospital API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
});

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    var skipDatabaseInitializer = builder.Configuration.GetValue<bool>("SKIP_DATABASE_INITIALIZER");
    if (skipDatabaseInitializer)
    {
        app.Logger.LogWarning("Skipping database initialization because SKIP_DATABASE_INITIALIZER is enabled.");
    }
    else
    {
        try
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync();
        }
        catch (Exception ex) when (ex.GetType().Namespace?.StartsWith("MongoDB", StringComparison.Ordinal) == true)
        {
            app.Logger.LogCritical(
                ex,
                "MongoDB connection failed for database '{DatabaseName}'. Update ConnectionStrings:DefaultConnection or MongoDb:DatabaseName before running the app.",
                mongoDatabaseName);
            throw;
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionMiddleware();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AngularClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CareCommunicationHub>("/hubs/care-communication");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
