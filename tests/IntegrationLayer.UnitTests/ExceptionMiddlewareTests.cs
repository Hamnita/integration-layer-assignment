using System.Text.Json;
using IntegrationLayer.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace IntegrationLayer.UnitTests;

public class ExceptionMiddlewareTests
{
    private readonly ExceptionMiddleware _sut = new();

    [Fact]
    public async Task InvokeAsync_Returns500WithJsonBody_WhenUnhandledExceptionThrown()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => throw new Exception("boom");

        await _sut.InvokeAsync(context, next);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenNoExceptionThrown()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }
}
