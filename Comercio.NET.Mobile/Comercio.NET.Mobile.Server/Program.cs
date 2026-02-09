using Comercio.NET.Mobile.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel para escuchar en localhost Y en la IP de red
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Parse("127.0.0.1"), 5000); // Localhost
    serverOptions.Listen(IPAddress.Parse("192.168.1.108"), 5000); // Tu IP específica
});

// Agregar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar servicio de arqueo
builder.Services.AddScoped<ArqueoCajaService>();

// CORS permisivo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();