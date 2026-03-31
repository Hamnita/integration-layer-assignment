using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace IntegrationLayer.Api.Middleware;

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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var extractedBytes = Encoding.UTF8.GetBytes(extractedApiKey.ToString());
        if (!CryptographicOperations.FixedTimeEquals(_apiKeyBytes, extractedBytes))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
