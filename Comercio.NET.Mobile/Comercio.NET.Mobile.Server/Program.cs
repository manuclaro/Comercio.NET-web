using Comercio.NET.Mobile.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar puerto según el entorno
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// En producción (Railway), escuchar en todas las interfaces
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Agregar HttpClient factory
builder.Services.AddHttpClient();

// Agregar servicios
builder.Services.AddControllers();

// Registrar servicios
builder.Services.AddScoped<ArqueoCajaService>();
builder.Services.AddScoped<AuthService>();

// CORS permisivo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Agregar logging
builder.Logging.AddConsole();

var app = builder.Build();

// Servir archivos estáticos (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Fallback a index.html para SPA
app.MapFallbackToFile("/index.html");

// Endpoint de salud
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