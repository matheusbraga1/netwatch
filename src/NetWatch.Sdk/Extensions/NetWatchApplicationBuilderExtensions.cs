using Microsoft.AspNetCore.Builder;
using NetWatch.Sdk.Middleware;

namespace NetWatch.Sdk.Extensions;

public static class NetWatchApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNetWatch(this IApplicationBuilder app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));
        
        return app.UseMiddleware<NetWatchMiddleware>();
    }
}
