using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetWatch.Sdk.Buffering;
using NetWatch.Sdk.Configuration;

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

        services.TryAddSingleton<IMetricsBuffer, MetricsBuffer>();

        return services;
    }
}
