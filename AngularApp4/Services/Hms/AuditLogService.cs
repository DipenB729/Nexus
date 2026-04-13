using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AngularApp4.Data;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Http;

namespace AngularApp4.Services.Hms;

public static class AuditLogCategories
{
    public const string Authentication = "Authentication";
    public const string MasterSetup = "MasterSetup";
    public const string Laboratory = "Laboratory";
    public const string Inventory = "Inventory";
    public const string StockAdjustment = "StockAdjustment";
    public const string Billing = "Billing";
    public const string RefundApproval = "RefundApproval";
    public const string Administration = "Administration";
}

public sealed class AuditLogRequest
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public long? EntityId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public object? Metadata { get; set; }
    public long? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string? PerformedByRole { get; set; }
    public string? ActorEmail { get; set; }
}

public interface IAuditLogService
{
    Task WriteAsync(AuditLogRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<AuditLogService> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Category) ||
            string.IsNullOrWhiteSpace(request.Action) ||
            string.IsNullOrWhiteSpace(request.Summary))
        {
            return;
        }

        try
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var connection = _httpContextAccessor.HttpContext?.Connection;
            var headers = _httpContextAccessor.HttpContext?.Request.Headers;

            var userId = request.PerformedByUserId ?? ParseLong(principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal?.FindFirstValue("sub"));
            var userName = request.PerformedByName
                ?? principal?.FindFirstValue(ClaimTypes.Name)
                ?? principal?.Identity?.Name;
            var role = request.PerformedByRole ?? principal?.FindFirstValue(ClaimTypes.Role);
            var actorEmail = request.ActorEmail
                ?? principal?.FindFirstValue(ClaimTypes.Email)
                ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

            var metadataJson = request.Metadata switch
            {
                null => null,
                string text => text,
                _ => JsonSerializer.Serialize(request.Metadata)
            };

            _db.AuditLogEntries.Add(new AuditLogEntry
            {
                Category = Truncate(request.Category.Trim(), 80) ?? string.Empty,
                Action = Truncate(request.Action.Trim(), 80) ?? string.Empty,
                EntityName = Truncate(request.EntityName?.Trim(), 120),
                EntityId = request.EntityId,
                TargetDisplayName = Truncate(request.TargetDisplayName?.Trim(), 180),
                Summary = Truncate(request.Summary.Trim(), 500) ?? string.Empty,
                MetadataJson = Truncate(metadataJson, 4000),
                PerformedByUserId = userId,
                PerformedByName = Truncate(userName?.Trim(), 150),
                PerformedByRole = Truncate(role?.Trim(), 80),
                ActorEmail = Truncate(actorEmail?.Trim(), 150),
                IpAddress = Truncate(connection?.RemoteIpAddress?.ToString(), 64),
                UserAgent = Truncate(headers?["User-Agent"].ToString(), 250),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist audit log entry for {Category}/{Action}", request.Category, request.Action);
        }
    }

    private static long? ParseLong(string? value)
    {
        return long.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
