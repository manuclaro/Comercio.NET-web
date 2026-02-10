var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

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



//using Comercio.NET.Mobile.Server.Services;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using System.Net;

//var builder = WebApplication.CreateBuilder(args);

//// Configurar puerto según el entorno
//var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

//// Solo configurar IPs específicas en desarrollo local
//if (builder.Environment.IsDevelopment())
//{
//    builder.WebHost.ConfigureKestrel(serverOptions =>
//    {
//        serverOptions.Listen(IPAddress.Parse("127.0.0.1"), 5000);
//        serverOptions.Listen(IPAddress.Parse("192.168.1.108"), 5000);
//    });
//}
//else
//{
//    // En producción (Railway), escuchar en todas las interfaces
//    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
//}

//// Agregar servicios
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// Registrar servicio de arqueo
//builder.Services.AddScoped<ArqueoCajaService>();

//// CORS permisivo
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll",
//        policy => policy
//            .AllowAnyOrigin()
//            .AllowAnyMethod()
//            .AllowAnyHeader());
//});

//var app = builder.Build();

//app.UseDefaultFiles();
//app.UseStaticFiles();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//// NO usar HTTPS redirection en Railway
//// app.UseHttpsRedirection();

//app.UseCors("AllowAll");
//app.UseAuthorization();
//app.MapControllers();
//app.MapFallbackToFile("/index.html");

//app.Run();