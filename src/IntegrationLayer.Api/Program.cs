using IntegrationLayer.Api.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP client to ExampleService microservice
builder.Services.AddHttpClient<IExampleServiceClient, ExampleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ExampleService"]
        ?? throw new InvalidOperationException("Services:ExampleService is not configured."));
});

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

public partial class Program { }
