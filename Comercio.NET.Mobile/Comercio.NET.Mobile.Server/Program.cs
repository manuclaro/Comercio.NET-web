using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddScoped<ArqueoCajaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IProductosService, ProductosService>();
builder.Services.AddScoped<EstadisticasService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IMesasService, MesasService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Logging.AddConsole();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");

app.MapGet("/api/health", () =>
{
    var sqlBridgeUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL");
    return Results.Ok(new
    {
        status = "OK",
        hasSqlBridgeUrl = !string.IsNullOrEmpty(sqlBridgeUrl),
        sqlBridgeUrl = sqlBridgeUrl
    });
});

app.Run();