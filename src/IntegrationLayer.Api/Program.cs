using IntegrationLayer.Api.Clients;
using IntegrationLayer.Core.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "API key required for all requests"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            []
        }
    });
});

builder.Services.AddTransient<InternalApiKeyHandler>();

// HTTP client to VehicleService microservice
builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
}).AddHttpMessageHandler<InternalApiKeyHandler>();

// HTTP client to InsuranceService microservice
builder.Services.AddHttpClient<IInsuranceServiceClient, InsuranceServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InsuranceService"]
        ?? throw new InvalidOperationException("Services:InsuranceService is not configured."));
}).AddHttpMessageHandler<InternalApiKeyHandler>();

builder.Services.AddSingleton<ApiKeyMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
