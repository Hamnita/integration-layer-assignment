using IntegrationLayer.Core.Middleware;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.UnitTests;

public class InternalApiKeyHandlerTests
{
    private const string TestInternalKey = "internal-key-456";

    [Fact]
    public void Constructor_Throws_WhenInternalApiKeyNotConfigured()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new InternalApiKeyHandler(emptyConfig));
    }

    [Fact]
    public async Task SendAsync_AddsApiKeyHeader_WithConfiguredValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Services:InternalApiKey"] = TestInternalKey })
            .Build();

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new TestMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new InternalApiKeyHandler(config) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("X-Api-Key"));
        Assert.Equal(TestInternalKey, capturedRequest.Headers.GetValues("X-Api-Key").Single());
    }

    [Fact]
    public async Task SendAsync_DoesNotOverwriteExistingHeader()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Services:InternalApiKey"] = TestInternalKey })
            .Build();

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new TestMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new InternalApiKeyHandler(config) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://test/");
        request.Headers.TryAddWithoutValidation("X-Api-Key", "already-set");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Contains("already-set", capturedRequest.Headers.GetValues("X-Api-Key"));
    }
}

internal class TestMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public TestMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}
