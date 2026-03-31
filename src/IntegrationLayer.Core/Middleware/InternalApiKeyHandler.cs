using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.Core.Middleware;

public class InternalApiKeyHandler : DelegatingHandler
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly string _apiKey;

    public InternalApiKeyHandler(IConfiguration configuration)
    {
        _apiKey = configuration["Services:InternalApiKey"]
            ?? throw new InvalidOperationException("Services:InternalApiKey is not configured.");
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(ApiKeyHeaderName, _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
