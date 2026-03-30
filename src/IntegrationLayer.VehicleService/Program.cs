using IntegrationLayer.VehicleService.Repositories;
using IntegrationLayer.VehicleService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IVehicleRepository, VehicleRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApi:BaseUrl"]
        ?? throw new InvalidOperationException("ExternalApi:BaseUrl is not configured."));
});

builder.Services.AddScoped<IVehicleService, VehicleService>();

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
