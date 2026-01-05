using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AngularApp4.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICES REGISTRATION (Must be before builder.Build()) ---

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ADD CORS HERE
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. BUILD THE APP ---
var app = builder.Build();

// --- 3. MIDDLEWARE PIPELINE (Order Matters!) ---

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // Routing first

// CORS MUST be between UseRouting and UseEndpoints (MapControllerRoute)
app.UseCors("AllowAll");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();