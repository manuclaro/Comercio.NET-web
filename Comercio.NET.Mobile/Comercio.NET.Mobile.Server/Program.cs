using Comercio.NET.Mobile.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar puerto según el entorno
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// En producción (Railway), escuchar en todas las interfaces
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Agregar servicios
builder.Services.AddControllers();

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

// Servir archivos estáticos (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Fallback a index.html para SPA
app.MapFallbackToFile("/index.html");

// Endpoint adicional si lo necesitas
app.MapGet("/misdatos", async () =>
{
    var baseUrl = Environment.GetEnvironmentVariable("SQL_BRIDGE_URL");

    try
    {
        var http = new HttpClient();
        var resp = await http.GetAsync($"{baseUrl}/datos");
        var txt = await resp.Content.ReadAsStringAsync();

        return Results.Content(txt, "application/json");
    }
    catch (Exception ex)
    {
        return Results.Content("ERROR: " + ex.ToString());
    }
});

app.Run();