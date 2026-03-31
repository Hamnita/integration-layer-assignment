using IntegrationLayer.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.UnitTests;

public class ApiKeyMiddlewareTests
{
    private const string ValidApiKey = "test-key-123";
    private readonly ApiKeyMiddleware _sut;

    public ApiKeyMiddlewareTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiKey"] = ValidApiKey })
            .Build();

        _sut = new ApiKeyMiddleware(configuration);
    }

    [Fact]
    public void Constructor_Throws_WhenApiKeyNotConfigured()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new ApiKeyMiddleware(emptyConfig));
    }

    [Fact]
    public async Task InvokeAsync_Returns401_WhenApiKeyHeaderMissing()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Returns401_WhenApiKeyHeaderValueIsWrong()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "wrong-key";
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenApiKeyHeaderIsCorrect()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = ValidApiKey;
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
