using Comercio.NET.Mobile.Server.Controllers;
using Comercio.NET.Mobile.Server.Services;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Configurar puerto según el entorno
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// En producción (Railway), escuchar en todas las interfaces
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Agregar HttpClient factory con handler que fuerza IPv4
builder.Services.AddHttpClient("default")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        ConnectCallback = async (context, cancellationToken) =>
        {
            var entries = await Dns.GetHostAddressesAsync(
                context.DnsEndPoint.Host,
                AddressFamily.InterNetwork,
                cancellationToken);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(entries[0], context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    });

// Registrar también el cliente sin nombre (usado por los servicios)
builder.Services.AddHttpClient();

// Agregar servicios
builder.Services.AddControllers();

// Registrar servicios
builder.Services.AddScoped<ArqueoCajaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IProductosService, ProductosService>();
builder.Services.AddScoped<EstadisticasService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IMesasService, MesasService>();

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