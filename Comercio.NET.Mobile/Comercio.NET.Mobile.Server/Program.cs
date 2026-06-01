using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Services;

// Configurar GC para entornos de contenedor (Railway/Linux).
// GCConserveMemory (0-9): a mayor valor, el GC es más agresivo devolviendo
// memoria al SO a costa de mayor frecuencia de recolección.
System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;
AppContext.SetData("GCConserveMemory", 5);

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddHttpClient();
// Aumentar HandlerLifetime para reducir rotaciones innecesarias de handlers
// (los servicios singleton reutilizan siempre el mismo HttpClient).
builder.Services.ConfigureHttpClientDefaults(b =>
    b.SetHandlerLifetime(TimeSpan.FromMinutes(30)));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy        = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddHostedService<Comercio.NET.Mobile.Server.Services.MemoryManagementService>();
builder.Services.AddSingleton<ArqueoCajaService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<IProductosService, ProductosService>();
builder.Services.AddSingleton<EstadisticasService>();
builder.Services.AddSingleton<IVentasService, VentasService>();
builder.Services.AddSingleton<IAuditoriaService, AuditoriaService>();
builder.Services.AddSingleton<ITurnoService, TurnoService>();
builder.Services.AddSingleton<DbService>();
builder.Services.AddSingleton<AfipService>();
builder.Services.AddSingleton<ICajaService, CajaService>();
builder.Services.AddSingleton<FacturasService>();

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