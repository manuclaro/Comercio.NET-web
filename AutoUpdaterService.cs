using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;

namespace Comercio.NET.Servicios
{
    /// <summary>
    /// Servicio para gestionar actualizaciones automáticas de la aplicación
    /// </summary>
    public class AutoUpdaterService : IDisposable
    {
        private readonly string _updateServerUrl;
        private readonly string _currentVersion;
        private readonly string _appPath;
        private readonly HttpClient _httpClient;

        // ? Archivos que NO se deben sobrescribir (configuraciones locales)
        private readonly string[] _archivosProtegidos = new[]
        {
            "appsettings.json",
            "appsettings.Production.json",
            "*.db",      // Bases de datos locales
            "*.log",     // Archivos de log
            "config.json" // Otras configuraciones
        };

        public AutoUpdaterService(string updateServerUrl, string currentVersion)
        {
            _updateServerUrl = updateServerUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(updateServerUrl));
            _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            _appPath = AppDomain.CurrentDomain.BaseDirectory;
            _httpClient = new HttpClient 
            { 
                Timeout = TimeSpan.FromMinutes(5) 
            };

            Debug.WriteLine($"[AUTO-UPDATE] Inicializado - Versión actual: {_currentVersion}");
            Debug.WriteLine($"[AUTO-UPDATE] Servidor: {_updateServerUrl}");
        }

        /// <summary>
        /// Verifica si hay una actualización disponible
        /// </summary>
        public async Task<VersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                Debug.WriteLine($"[AUTO-UPDATE] Verificando actualizaciones...");
                
                var versionUrl = $"{_updateServerUrl}/version.json";
                var response = await _httpClient.GetStringAsync(versionUrl);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(response, options);

                if (versionInfo != null)
                {
                    Debug.WriteLine($"[AUTO-UPDATE] Versión remota: {versionInfo.Version}");
                    
                    if (IsNewerVersion(versionInfo.Version, _currentVersion))
                    {
                        Debug.WriteLine($"[AUTO-UPDATE] ? Nueva versión disponible: {versionInfo.Version}");
                        return versionInfo;
                    }
                    else
                    {
                        Debug.WriteLine($"[AUTO-UPDATE] ?? Ya tienes la última versión");
                    }
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[AUTO-UPDATE] ?? Error de conexión: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTO-UPDATE] ? Error verificando actualizaciones: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Descarga e instala la actualización
        /// </summary>
        public async Task<bool> DownloadAndInstallAsync(VersionInfo versionInfo, IProgress<int> progress = null)
        {
            try
            {
                Debug.WriteLine($"[AUTO-UPDATE] Iniciando descarga de versión {versionInfo.Version}");

                // 1. Crear carpeta temporal
                var tempFolder = Path.Combine(Path.GetTempPath(), "ComercioNET_Update_" + DateTime.Now.Ticks);
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
                Directory.CreateDirectory(tempFolder);

                Debug.WriteLine($"[AUTO-UPDATE] Carpeta temporal: {tempFolder}");

                // 2. Descargar archivo de actualización
                var zipPath = Path.Combine(tempFolder, "update.zip");
                await DownloadFileAsync(versionInfo.DownloadUrl, zipPath, progress);

                Debug.WriteLine($"[AUTO-UPDATE] ? Descarga completada: {new FileInfo(zipPath).Length / 1024 / 1024:F2} MB");

                // 3. Hacer backup de configuraciones
                var backupFolder = Path.Combine(tempFolder, "backup_config");
                Directory.CreateDirectory(backupFolder);
                BackupConfigFiles(backupFolder);

                // 4. Extraer archivos
                var extractFolder = Path.Combine(tempFolder, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractFolder);

                Debug.WriteLine($"[AUTO-UPDATE] ? Archivos extraídos en: {extractFolder}");

                // 5. Crear script de actualización
                CreateUpdateScript(extractFolder, backupFolder, versionInfo.Version);

                // 6. Ejecutar actualizador y cerrar aplicación
                ExecuteUpdater();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTO-UPDATE] ? Error en actualización: {ex.Message}");
                MessageBox.Show($"Error durante la actualización:\n\n{ex.Message}\n\nPor favor, contacte al soporte técnico.",
                    "Error de Actualización", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath, IProgress<int> progress)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
                        Debug.WriteLine($"[AUTO-UPDATE]   ? Backup: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AUTO-UPDATE]   ?? Error respaldando {pattern}: {ex.Message}");
                }
            }

            Debug.WriteLine($"[AUTO-UPDATE] ? {archivosCopia} archivos respaldados");
        }

        private void CreateUpdateScript(string extractFolder, string backupFolder, string newVersion)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "update_comercio.bat");
            
            var script = $@"@echo off
chcp 65001 >nul
title Actualizando Comercio .NET a v{newVersion}
color 0A

echo ??????????????????????????????????????????????????????????
echo ?                                                        ?
echo ?     ACTUALIZANDO COMERCIO .NET                         ?
echo ?     Versión: {newVersion}                                      ?
echo ?                                                        ?
echo ??????????????????????????????????????????????????????????
echo.

REM Esperar a que la aplicación se cierre completamente
echo [1/4] Cerrando aplicación anterior...
timeout /t 3 /nobreak >nul
echo       ? Completado
echo.

REM Copiar archivos actualizados
echo [2/4] Instalando archivos nuevos...
xcopy ""{extractFolder}\*"" ""{_appPath}"" /E /Y /I /Q /H
if errorlevel 1 (
    echo       ? Error copiando archivos
    pause
    exit /b 1
)
echo       ? Completado
echo.

REM Restaurar archivos de configuración
echo [3/4] Restaurando configuraciones locales...
xcopy ""{backupFolder}\*"" ""{_appPath}"" /E /Y /I /Q
if errorlevel 1 (
    echo       ? Advertencia: No se pudieron restaurar todas las configuraciones
)
echo       ? Completado
echo.

echo [4/4] Reiniciando aplicación...
timeout /t 2 /nobreak >nul

REM Reiniciar aplicación
start """" ""{Path.Combine(_appPath, "Comercio.NET.exe")}""

echo.
echo ??????????????????????????????????????????????????????????
echo ?                                                        ?
echo ?     ? ACTUALIZACIÓN COMPLETADA                         ?
echo ?                                                        ?
echo ??????????????????????????????????????????????????????????

REM Limpiar archivos temporales
timeout /t 3 /nobreak >nul
rd /s /q ""{Path.GetDirectoryName(extractFolder)}""
del ""{scriptPath}""
exit
";

            File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);
            Debug.WriteLine($"[AUTO-UPDATE] ? Script de actualización creado: {scriptPath}");
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

            Debug.WriteLine($"[AUTO-UPDATE] ?? Ejecutando actualizador...");
            Process.Start(startInfo);
            
            // Cerrar la aplicación actual
            Debug.WriteLine($"[AUTO-UPDATE] ?? Cerrando aplicación para actualizar...");
            Application.Exit();
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
                Debug.WriteLine($"[AUTO-UPDATE] ?? Error comparando versiones: '{remoteVersion}' vs '{localVersion}'");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Información de versión disponible en el servidor
    /// </summary>
    public class VersionInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string[] ChangeLog { get; set; }
        public bool IsRequired { get; set; } // Si es obligatoria
        public long FileSize { get; set; } // Tamaño del archivo en bytes
    }
}