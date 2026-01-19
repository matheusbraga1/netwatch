using NetWatch.Collector.HealthChecks;
using NetWatch.Collector.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "NetWatch Collector API",
        Version = "v1",
        Description = "API for ingesting metrics from NetWatch SDK"
    });

    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "API Key authentication"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    try
    {
        var config = ConfigurationOptions.Parse(redisConnectionString);
        config.AbortOnConnectFail = false;
        config.ConnectRetry = 3;
        config.ConnectTimeout = 5000;
        config.SyncTimeout = 5000;

        var connection = ConnectionMultiplexer.Connect(config);

        connection.ConnectionFailed += (sender, args) =>
        {
            logger.LogError($"Redis connection failed: {args.Exception?.Message ?? "Unknown error"}");
        };

        connection.ConnectionRestored += (sender, args) =>
        {
            logger.LogInformation("Redis connection restored");
        };

        logger.LogInformation($"Connected to Redis at {redisConnectionString}");

        return connection;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Failed to connect to Redis at {redisConnectionString}");
        throw;
    }
});

builder.Services.AddSingleton<IQueueService, RedisQueueService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
     {
         policy.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
     });
});

builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetWatch Collector API v1");
    });
}

app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
