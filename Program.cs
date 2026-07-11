using PhoneCallApi.API.Controllers;
using PhoneCallApi.Application.Contracts;
using PhoneCallApi.Application.Services;
using PhoneCallApi.Infrastructure.Modem;
using PhoneCallApi.API.Gateway.Interfaces;
using PhoneCallApi.API.Gateway.Middleware;
using PhoneCallApi.API.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Phone Call API", Version = "v1" });
    
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "API Key authentication"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// Конфигурация
builder.Services.Configure<ModemSettings>(builder.Configuration.GetSection("ModemSettings"));
builder.Services.Configure<ApiGatewaySettings>(builder.Configuration.GetSection("ApiGatewaySettings"));

// Memory Cache для rate limiting
builder.Services.AddMemoryCache();

// Gateway Services
builder.Services.AddSingleton<IApiGatewayService, ApiGatewayService>();

// Dependency Injection
builder.Services.AddSingleton<IModemService, MegafonService>();
builder.Services.AddScoped<ICallService, CallService>();

var app = builder.Build();

// Gateway Middleware
app.UseMiddleware<ApiGatewayMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
