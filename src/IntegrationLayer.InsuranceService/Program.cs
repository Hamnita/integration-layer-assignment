using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;
using IntegrationLayer.InsuranceService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IInsuranceRepository, InsuranceRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApi:BaseUrl"]
        ?? throw new InvalidOperationException("ExternalApi:BaseUrl is not configured."));
});

builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
});

builder.Services.AddScoped<IInsuranceService, InsuranceService>();

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
