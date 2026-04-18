using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace Nexus.Hosting.AspNetCore;

public static class PostgresConnectionStringExtensions
{
    public static string NormalizePostgresConnectionString(this string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(userInfo[0]);

            if (userInfo.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(userInfo[1]);
            }
        }

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("sslmode", out var sslModeValue) &&
                !string.IsNullOrWhiteSpace(sslModeValue) &&
                Enum.TryParse<SslMode>(sslModeValue.ToString(), true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }

            if (query.TryGetValue("trust server certificate", out var trustServerCertificateValue) &&
                !string.IsNullOrWhiteSpace(trustServerCertificateValue) &&
                bool.TryParse(trustServerCertificateValue.ToString(), out var trustServerCertificate))
            {
                builder.TrustServerCertificate = trustServerCertificate;
            }
        }

        return builder.ConnectionString;
    }
}
