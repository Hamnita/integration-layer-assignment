using IntegrationLayer.Core.Interfaces.Repositories;
using IntegrationLayer.Core.Interfaces.Services;
using IntegrationLayer.Repositories;
using IntegrationLayer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repositories (HTTP clients)
builder.Services.AddHttpClient<IExampleRepository, ExampleRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApi:BaseUrl"]
        ?? throw new InvalidOperationException("ExternalApi:BaseUrl is not configured."));
});

// Services
builder.Services.AddScoped<IExampleService, ExampleService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
