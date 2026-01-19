using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetWatch.Sdk.Buffering;
using NetWatch.Sdk.Configuration;
using NetWatch.Sdk.Transport;

namespace NetWatch.Sdk.Extensions;

public static class NetWatchServiceCollectionExtensions
{
    public static IServiceCollection AddNetWatch(
        this IServiceCollection services,
        Action<NetWatchOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // ✅ Registra configurações
        services.Configure(configureOptions);

        // ✅ Validação
        services.PostConfigure<NetWatchOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new InvalidOperationException("NetWatch API Key is required");

            if (string.IsNullOrWhiteSpace(options.CollectorEndpoint))
                throw new InvalidOperationException("NetWatch Collector Endpoint is required");

            if (options.FlushIntervalSeconds <= 0)
                throw new InvalidOperationException("FlushIntervalSeconds must be greater than 0");

            if (options.MaxBufferSize <= 0)
                throw new InvalidOperationException("MaxBufferSize must be greater than 0");

            if (options.SampleRate < 0.0 || options.SampleRate > 1.0)
                throw new InvalidOperationException("SampleRate must be between 0.0 and 1.0");
        });

        // ✅ Registra HttpClient factory
        services.AddHttpClient();

        // ✅ Registra Transport manualmente (resolve o problema de scoping)
        services.TryAddSingleton<IMetricsTransport>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("NetWatch");
            var options = sp.GetRequiredService<IOptions<NetWatchOptions>>();
            var logger = sp.GetRequiredService<ILogger<HttpMetricsTransport>>();

            return new HttpMetricsTransport(httpClient, options, logger);
        });

        // ✅ Registra buffer como Singleton
        services.TryAddSingleton<IMetricsBuffer, MetricsBuffer>();

        return services;
    }
}
