using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace Comercio.NET.Servicios
{
    /// <summary>
    /// Servicio para gestionar actualizaciones automaticas de la aplicacion
    /// usando GitHub Releases como servidor de actualizaciones.
    /// 
    /// Flujo:
    /// 1. El desarrollador hace push a la rama y crea un GitHub Release (ej: v1.4.0)
    /// 2. Adjunta el .zip con los binarios compilados al Release
    /// 3. Los clientes consultan la API de GitHub para detectar nuevas versiones
    /// 4. Si hay version nueva, descargan el .zip y aplican la actualizacion
    /// </summary>
    public class AutoUpdaterService : IDisposable
    {
        private readonly string _githubOwner;
        private readonly string _githubRepo;
        private readonly string _currentVersion;
        private readonly string _appPath;
        private readonly HttpClient _httpClient;

        // Archivos que NO se deben sobrescribir (configuraciones locales)
        private readonly string[] _archivosProtegidos = new[]
        {
            "appsettings.json",
            "appsettings.Production.json",
            "loginconfig.json",
            "*.db",
            "*.log",
            "config.json",
            "debug_auth.txt",
            "*.pfx"
            // version.txt NO está aquí: se escribe al final del .bat intencionalmente
        };

        public AutoUpdaterService(string githubRepoUrl, string currentVersion, string githubToken = null)
        {
            if (string.IsNullOrWhiteSpace(githubRepoUrl))
                throw new ArgumentNullException(nameof(githubRepoUrl));

            _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            _appPath = AppDomain.CurrentDomain.BaseDirectory;

            // Parsear owner/repo de la URL de GitHub
            // Acepta formatos: 
            //   https://github.com/owner/repo
            //   owner/repo
            var cleaned = githubRepoUrl
                .Replace("https://github.com/", "")
                .Replace("http://github.com/", "")
                .Trim('/');

            // Remover cualquier path adicional despues de owner/repo
            var parts = cleaned.Split('/');

            if (parts.Length >= 2)
            {
                _githubOwner = parts[0];
                _githubRepo = parts[1];
            }
            else
            {
                throw new ArgumentException(
                    "URL de repositorio GitHub invalida. Use formato: https://github.com/owner/repo",
                    nameof(githubRepoUrl));
            }

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ComercioNET-Updater", currentVersion));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            // Si el repositorio es privado, se necesita un token de acceso personal (PAT)
            if (!string.IsNullOrWhiteSpace(githubToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", githubToken);
                Debug.WriteLine($"[AUTO-UPDATE] Token de GitHub configurado para repo privado");
            }

            Debug.WriteLine($"[AUTO-UPDATE] Inicializado - Version actual: {_currentVersion}");
            Debug.WriteLine($"[AUTO-UPDATE] Repositorio: {_githubOwner}/{_githubRepo}");
        }

        /// <summary>
        /// Verifica si hay una actualizacion disponible consultando GitHub Releases
        /// </summary>
        public async Task<VersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                Debug.WriteLine($"[AUTO-UPDATE] Verificando actualizaciones en GitHub...");

                var apiUrl = $"https://api.github.com/repos/{_githubOwner}/{_githubRepo}/releases/latest";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Debug.WriteLine("[AUTO-UPDATE] No hay releases publicados en el repositorio.");
                        return null;
                    }

                    Debug.WriteLine($"[AUTO-UPDATE] Error HTTP: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, options);

                if (release == null)
                    return null;

                // Extraer version del tag (quitar prefijo "v" si existe)
                string remoteVersion = release.TagName?.TrimStart('v', 'V') ?? "";

                Debug.WriteLine($"[AUTO-UPDATE] Version remota: {remoteVersion} (tag: {release.TagName})");

                if (!IsNewerVersion(remoteVersion, _currentVersion))
                {
                    Debug.WriteLine("[AUTO-UPDATE] Ya tienes la ultima version.");
                    return null;
                }

                // Buscar el asset .zip adjunto al release
                var zipAsset = release.Assets?.FirstOrDefault(a =>
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                if (zipAsset == null)
                {
                    Debug.WriteLine("[AUTO-UPDATE] No se encontro archivo .zip en el release.");
                    return null;
                }

                Debug.WriteLine($"[AUTO-UPDATE] Nueva version disponible: {remoteVersion}");
                Debug.WriteLine($"[AUTO-UPDATE] Archivo: {zipAsset.Name} ({zipAsset.Size / 1024.0 / 1024.0:F2} MB)");

                // Construir changelog desde el body del release
                var changeLog = ParseChangeLog(release.Body);

                // Para repos privados usar la URL de la API; para públicos usar browser_download_url
                var downloadUrl = _httpClient.DefaultRequestHeaders.Authorization != null
                    ? zipAsset.Url  // URL de la API (funciona con token para repos privados)
                    : zipAsset.BrowserDownloadUrl; // URL directa (funciona para repos públicos)

                return new VersionInfo
                {
                    Version = remoteVersion,
                    DownloadUrl = downloadUrl,
                    ReleaseDate = release.PublishedAt ?? release.CreatedAt,
                    ChangeLog = changeLog,
                    IsRequired = release.Body?.Contains("[OBLIGATORIA]", StringComparison.OrdinalIgnoreCase) == true,
                    HasMigrations = release.Body?.Contains("[DB-MIGRATION]", StringComparison.OrdinalIgnoreCase) == true,
                    FileSize = zipAsset.Size
                };
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[AUTO-UPDATE] Error de conexion: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("[AUTO-UPDATE] Timeout al verificar actualizaciones.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTO-UPDATE] Error verificando actualizaciones: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Descarga e instala la actualizacion
        /// </summary>
        public async Task<bool> DownloadAndInstallAsync(VersionInfo versionInfo, IProgress<int> progress = null)
        {
            try
            {
                Debug.WriteLine($"[AUTO-UPDATE] Iniciando descarga de version {versionInfo.Version}");

                // 1. Crear carpeta temporal
                var tempFolder = Path.Combine(Path.GetTempPath(), "ComercioNET_Update_" + DateTime.Now.Ticks);
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
                Directory.CreateDirectory(tempFolder);

                Debug.WriteLine($"[AUTO-UPDATE] Carpeta temporal: {tempFolder}");

                // 2. Descargar archivo de actualizacion
                var zipPath = Path.Combine(tempFolder, "update.zip");
                await DownloadFileAsync(versionInfo.DownloadUrl, zipPath, progress);

                Debug.WriteLine($"[AUTO-UPDATE] Descarga completada: {new FileInfo(zipPath).Length / 1024.0 / 1024.0:F2} MB");

                // 3. Hacer backup de configuraciones
                var backupFolder = Path.Combine(tempFolder, "backup_config");
                Directory.CreateDirectory(backupFolder);
                BackupConfigFiles(backupFolder);

                // 4. Extraer archivos
                var extractFolder = Path.Combine(tempFolder, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractFolder);

                Debug.WriteLine($"[AUTO-UPDATE] Archivos extraidos en: {extractFolder}");

                // 5. Crear script de actualizacion
                CreateUpdateScript(extractFolder, backupFolder, versionInfo.Version);

                // 6. Ejecutar actualizador y cerrar aplicacion
                ExecuteUpdater();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTO-UPDATE] Error en actualizacion: {ex.Message}");
                MessageBox.Show(
                    $"Error durante la actualizacion:\n\n{ex.Message}\n\nPor favor, contacte al soporte tecnico.",
                    "Error de Actualizacion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private string[] ParseChangeLog(string releaseBody)
        {
            if (string.IsNullOrWhiteSpace(releaseBody))
                return new[] { "- Mejoras generales de rendimiento", "- Correcciones de errores" };

            var lines = releaseBody.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l =>
                {
                    if (l.StartsWith("- ") || l.StartsWith("* "))
                        return "- " + l.Substring(2);
                    if (l.StartsWith("## ") || l.StartsWith("### "))
                        return l.Replace("#", "").Trim();
                    return l;
                })
                .Where(l => !l.StartsWith("[OBLIGATORIA]", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return lines.Length > 0 ? lines : new[] { "- Mejoras generales de rendimiento", "- Correcciones de errores" };
        }

        private async Task DownloadFileAsync(string url, string destinationPath, IProgress<int> progress)
        {
            // Para descargar assets de la API de GitHub se necesita Accept: application/octet-stream
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (url.Contains("api.github.com"))
            {
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progress != null;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    var progressPercentage = (int)((totalRead * 100) / totalBytes);
                    progress.Report(progressPercentage);
                }
            }
        }

        private void BackupConfigFiles(string backupFolder)
        {
            Debug.WriteLine($"[AUTO-UPDATE] Respaldando configuraciones...");
            int archivosCopia = 0;

            foreach (var pattern in _archivosProtegidos)
            {
                try
                {
                    var files = Directory.GetFiles(_appPath, pattern, SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var destPath = Path.Combine(backupFolder, fileName);
                        File.Copy(file, destPath, true);
                        archivosCopia++;
                        Debug.WriteLine($"[AUTO-UPDATE]   Backup: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AUTO-UPDATE]   Error respaldando {pattern}: {ex.Message}");
                }
            }

            Debug.WriteLine($"[AUTO-UPDATE] {archivosCopia} archivos respaldados");
        }

        private void CreateUpdateScript(string extractFolder, string backupFolder, string newVersion)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "update_comercio.bat");
            var migrationsFolder = Path.Combine(_appPath, "migrations");
            var appExe = Path.Combine(_appPath, "Comercio .NET.exe");

            // Obtener PID del proceso actual para que el script espere a que termine
            var currentPid = Process.GetCurrentProcess().Id;

            // Bloque de migraciones: llama a la misma app con --migrate (modo consola, sin UI)
            var bloquesMigracion = $@"
REM === MIGRACIONES DE BASE DE DATOS ===
echo [4/6] Verificando migraciones de base de datos...
if exist ""{migrationsFolder}\migrate_*.sql"" (
    echo       Scripts SQL encontrados, ejecutando migraciones...
    ""{appExe}"" --migrate
    if errorlevel 1 (
        echo.
        echo   ERROR: La migracion de base de datos fallo.
        echo.
        echo   Contenido del log:
        echo   ----------------------------------------
        if exist ""{migrationsFolder}\migration.log"" (
            type ""{migrationsFolder}\migration.log""
        ) else (
            echo   Log no disponible
        )
        echo   ----------------------------------------
        echo.
        echo   Ruta del log: {migrationsFolder}\migration.log
        echo   La aplicacion NO sera reiniciada hasta que resuelva el error.
        echo.
        pause
        exit /b 1
    )
    echo       Migraciones aplicadas correctamente
) else (
    echo       No hay scripts de migracion pendientes
)
echo.";

            var script = $@"@echo off
chcp 65001 >nul
title Actualizando Comercio .NET a v{newVersion}
color 0A

echo ============================================================
echo.
echo      ACTUALIZANDO COMERCIO .NET
echo      Version: {newVersion}
echo.
echo ============================================================
echo.

REM Esperar a que la aplicacion se cierre completamente
echo [1/6] Esperando que la aplicacion se cierre...
:WAIT_LOOP
tasklist /FI ""PID eq {currentPid}"" 2>nul | find """" {currentPid}"""" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto WAIT_LOOP
)
echo       Aplicacion cerrada
echo.

REM Espera adicional por seguridad
timeout /t 2 /nobreak >nul

REM Copiar archivos actualizados (incluye la carpeta migrations\ si existe en el zip)
echo [2/6] Instalando archivos nuevos...
xcopy ""{extractFolder}\*"" ""{_appPath}"" /E /Y /I /Q /H
if errorlevel 1 (
    echo       Error copiando archivos
    pause
    exit /b 1
)
echo       Completado
echo.

REM Restaurar archivos de configuracion
echo [3/6] Restaurando configuraciones locales...
xcopy ""{backupFolder}\*"" ""{_appPath}"" /E /Y /I /Q
if errorlevel 1 (
    echo       Advertencia: No se pudieron restaurar todas las configuraciones
)
echo       Completado
echo.
{bloquesMigracion}
REM Escribir version.txt AL FINAL, despues de todo, para que nada lo pise
REM Este es el paso critico: garantiza que la app arranque con la version correcta
(echo|set /p=""{newVersion}"") > ""{_appPath}version.txt""
echo       Version actualizada: {newVersion}
echo.

echo [5/6] Reiniciando aplicacion...
timeout /t 2 /nobreak >nul

REM Reiniciar aplicacion
start """" ""{Path.Combine(_appPath, "Comercio .NET.exe")}""

echo.
echo ============================================================
echo.
echo      [6/6] ACTUALIZACION COMPLETADA
echo.
echo ============================================================

REM Limpiar archivos temporales
timeout /t 3 /nobreak >nul
rd /s /q ""{Path.GetDirectoryName(extractFolder)}""
del ""{scriptPath}""
exit
";

            File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);
            Debug.WriteLine($"[AUTO-UPDATE] Script de actualizacion creado: {scriptPath}");
        }

        private void ExecuteUpdater()
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "update_comercio.bat");

            var startInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                CreateNoWindow = false,
                WorkingDirectory = _appPath
            };

            Debug.WriteLine($"[AUTO-UPDATE] Ejecutando actualizador...");
            Process.Start(startInfo);

            Debug.WriteLine($"[AUTO-UPDATE] Cerrando aplicacion para actualizar...");
            Environment.Exit(0);
        }

        private bool IsNewerVersion(string remoteVersion, string localVersion)
        {
            try
            {
                var remote = new Version(remoteVersion);
                var local = new Version(localVersion);
                return remote > local;
            }
            catch
            {
                Debug.WriteLine($"[AUTO-UPDATE] Error comparando versiones: '{remoteVersion}' vs '{localVersion}'");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Informacion de version disponible para actualizar
    /// </summary>
    public class VersionInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string[] ChangeLog { get; set; }
        public bool IsRequired { get; set; }
        public long FileSize { get; set; }
        /// <summary>
        /// Indica que este release incluye scripts de migración de base de datos
        /// (detectado por la etiqueta [DB-MIGRATION] en el body del release).
        /// </summary>
        public bool HasMigrations { get; set; }
    }

    /// <summary>
    /// Modelo para deserializar la respuesta de GitHub Releases API
    /// </summary>
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; }
    }

    /// <summary>
    /// Modelo para los assets adjuntos a un GitHub Release
    /// </summary>
    public class GitHubAsset
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }
    }
}
