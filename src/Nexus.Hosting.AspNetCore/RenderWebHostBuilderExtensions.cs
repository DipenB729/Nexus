using Microsoft.AspNetCore.Hosting;

namespace Nexus.Hosting.AspNetCore;

public static class RenderWebHostBuilderExtensions
{
    public static IWebHostBuilder UseRenderPortBinding(this IWebHostBuilder webHostBuilder)
    {
        var renderPort = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(renderPort))
        {
            webHostBuilder.UseUrls($"http://*:{renderPort}");
        }

        return webHostBuilder;
    }
}
