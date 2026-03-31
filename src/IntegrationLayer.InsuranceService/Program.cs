using IntegrationLayer.Core.Middleware;
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;
using IntegrationLayer.InsuranceService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();

builder.Services.AddTransient<InternalApiKeyHandler>();
builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
}).AddHttpMessageHandler<InternalApiKeyHandler>();

builder.Services.AddScoped<IInsuranceService, InsuranceService>();
builder.Services.AddSingleton<ApiKeyMiddleware>();
builder.Services.AddSingleton<ExceptionMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
