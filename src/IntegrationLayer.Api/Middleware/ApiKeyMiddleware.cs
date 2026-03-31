using Microsoft.AspNetCore.Http;

namespace IntegrationLayer.Api.Middleware;

public class ApiKeyMiddleware : IMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var apiKey = _configuration["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey is not configured.");

        if (!apiKey.Equals(extractedApiKey.ToString(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
