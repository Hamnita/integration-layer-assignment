using System.Text.Json;
using IntegrationLayer.Core.Middleware;
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

    [Fact]
    public async Task InvokeAsync_WritesJsonErrorBody_WhenApiKeyHeaderMissing()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => Task.CompletedTask;

        await _sut.InvokeAsync(context, next);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WritesJsonErrorBody_WhenApiKeyHeaderIsWrong()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Headers["X-Api-Key"] = "wrong-key";
        RequestDelegate next = _ => Task.CompletedTask;

        await _sut.InvokeAsync(context, next);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
        Assert.Equal("application/json", context.Response.ContentType);
    }
}
