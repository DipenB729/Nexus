using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Nexus.Hosting.AspNetCore;

public static class ForwardedHeadersExtensions
{
    public static IApplicationBuilder UseCommonForwardedHeaders(this IApplicationBuilder app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        return app;
    }
}
