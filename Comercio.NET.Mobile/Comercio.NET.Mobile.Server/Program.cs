using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy        = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddSingleton<ArqueoCajaService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<IProductosService, ProductosService>();
builder.Services.AddSingleton<EstadisticasService>();
builder.Services.AddSingleton<IVentasService, VentasService>();
builder.Services.AddSingleton<IAuditoriaService, AuditoriaService>();
builder.Services.AddSingleton<IMesasService, MesasService>();
builder.Services.AddSingleton<ITurnoService, TurnoService>();

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
        status          = "OK",
        hasSqlBridgeUrl = !string.IsNullOrEmpty(sqlBridgeUrl),
        sqlBridgeUrl    = sqlBridgeUrl
    });
});

app.Run();