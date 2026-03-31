using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.Core.Middleware;

public class ApiKeyMiddleware : IMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly byte[] _apiKeyBytes;

    public ApiKeyMiddleware(IConfiguration configuration)
    {
        var apiKey = configuration["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey is not configured.");
        _apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            await WriteUnauthorized(context, "API key is missing.");
            return;
        }

        var extractedBytes = Encoding.UTF8.GetBytes(extractedApiKey.ToString());
        if (!CryptographicOperations.FixedTimeEquals(_apiKeyBytes, extractedBytes))
        {
            await WriteUnauthorized(context, "Invalid API key.");
            return;
        }

        await next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
