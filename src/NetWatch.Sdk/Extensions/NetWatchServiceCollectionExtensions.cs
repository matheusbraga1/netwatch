using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NetWatch.Sdk.Buffering;
using NetWatch.Sdk.Configuration;
using NetWatch.Sdk.Transport;

namespace NetWatch.Sdk.Extensions;

public static class NetWatchServiceCollectionExtensions
{
    public static IServiceCollection AddNetWatch(
        this IServiceCollection services,
        Action<NetWatchOptions>? configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);

        services.AddOptions<NetWatchOptions>()
            .Validate(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                    return false;

                if (string.IsNullOrWhiteSpace(options.CollectorEndpoint))
                    return false;

                if (options.FlushIntervalSeconds <= 0)
                    return false;

                if (options.MaxBufferSize <= 0)
                    return false;

                if (options.SampleRate < 0.0 || options.SampleRate > 1.0)
                    return false;

                return true;
            }, "Invalid NetWatch configuration");

        services.AddHttpClient<IMetricsTransport, HttpMetricsTransport>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<NetWatchOptions>>().Value;

                client.BaseAddress = new Uri(options.CollectorEndpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "NetWatch-SDK/1.0.0");
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.TryAddSingleton<IMetricsBuffer, MetricsBuffer>();

        return services;
    }
}
