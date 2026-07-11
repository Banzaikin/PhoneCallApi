using PhoneCallApi.API.Controllers;
using PhoneCallApi.Application.Contracts;
using PhoneCallApi.Application.Services;
using PhoneCallApi.Infrastructure.Modem;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration
builder.Services.Configure<ModemSettings>(builder.Configuration.GetSection("ModemSettings"));

// Dependency Injection
// Infrastructure implementation (зависит от Application контракта)
builder.Services.AddSingleton<IModemService, MegafonService>();

// Application services
builder.Services.AddScoped<ICallService, CallService>();

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