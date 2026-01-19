using NetWatch.Sdk.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetWatch(options =>
{
    options.ApiKey = "nw_example_key_12345";
    options.CollectorEndpoint = "http://localhost:5001";
    options.FlushIntervalSeconds = 5;
    options.MaxBufferSize = 10;
    options.SampleRate = 1.0;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "NetWatch Example API",
        Version = "v1",
        Description = "API de exemplo para demonstrar o NetWatch SDK"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetWatch Example API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseNetWatch();

app.MapGet("/api/test/fast", () =>
{
    return Results.Ok(new
    {
        message = "Fast endpoint - responde imediatamente",
        timestamp = DateTime.UtcNow,
        duration = "~10ms"
    });
})
.WithName("TestFast")
.WithTags("Test")
.WithOpenApi();

// 2. Endpoint lento (teste de performance)
app.MapGet("/api/test/slow", async () =>
{
    var delay = Random.Shared.Next(500, 2000); // 0.5 a 2 segundos
    await Task.Delay(delay);

    return Results.Ok(new
    {
        message = "Slow endpoint - resposta atrasada",
        timestamp = DateTime.UtcNow,
        delayMs = delay
    });
})
.WithName("TestSlow")
.WithTags("Test")
.WithOpenApi();

// 3. Endpoint com erro (teste de exception handling)
app.MapGet("/api/test/error", () =>
{
    throw new InvalidOperationException("Este é um erro intencional para testar captura de exceções pelo NetWatch");
})
.WithName("TestError")
.WithTags("Test")
.WithOpenApi();

// 4. Endpoint com 404 (teste de not found)
app.MapGet("/api/test/notfound", () =>
{
    return Results.NotFound(new
    {
        message = "Recurso não encontrado",
        timestamp = DateTime.UtcNow
    });
})
.WithName("TestNotFound")
.WithTags("Test")
.WithOpenApi();

// 5. Endpoint com POST (teste de diferentes métodos HTTP)
app.MapPost("/api/test/create", (CreateRequest request) =>
{
    return Results.Created($"/api/test/{Guid.NewGuid()}", new
    {
        message = "Recurso criado com sucesso",
        data = request,
        timestamp = DateTime.UtcNow
    });
})
.WithName("TestCreate")
.WithTags("Test")
.WithOpenApi();

// 6. Endpoint com query parameters
app.MapGet("/api/test/query", (string? name, int? age) =>
{
    return Results.Ok(new
    {
        message = "Query parameters recebidos",
        name = name ?? "não fornecido",
        age = age ?? 0,
        timestamp = DateTime.UtcNow
    });
})
.WithName("TestQuery")
.WithTags("Test")
.WithOpenApi();

// 7. Endpoint com path parameter
app.MapGet("/api/test/users/{id:int}", (int id) =>
{
    return Results.Ok(new
    {
        message = "Usuário encontrado",
        userId = id,
        userName = $"User_{id}",
        timestamp = DateTime.UtcNow
    });
})
.WithName("TestUserById")
.WithTags("Test")
.WithOpenApi();

// 8. Health check (este será ignorado pelo NetWatch por padrão)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithTags("Health")
    .ExcludeFromDescription();

app.Run();

public record CreateRequest(string Name, string Description);
