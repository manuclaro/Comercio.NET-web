@echo off
chcp 65001 >nul
title Empaquetar Comercio .NET para Actualización

echo ╔════════════════════════════════════════════════════════╗
echo ║                                                        ║
echo ║     EMPAQUETADOR DE ACTUALIZACIONES                    ║
echo ║     Comercio .NET                                      ║
echo ║                                                        ║
echo ╚════════════════════════════════════════════════════════╝
echo.

REM Solicitar versión
set /p VERSION="Ingrese el número de versión (ej: 1.3.0): "

if "%VERSION%"=="" (
    echo Error: Debe ingresar un número de versión
    pause
    exit /b 1
)

echo.
echo Versión: %VERSION%
echo.

REM Crear carpeta de salida
set OUTPUT_DIR=Releases\v%VERSION%
if exist "%OUTPUT_DIR%" (
    echo La carpeta %OUTPUT_DIR% ya existe.
    set /p OVERWRITE="¿Desea sobrescribirla? (S/N): "
    if /i not "%OVERWRITE%"=="S" (
        echo Operación cancelada
        pause
        exit /b 0
    )
    rd /s /q "%OUTPUT_DIR%"
)

mkdir "%OUTPUT_DIR%"

echo.
echo [1/4] Compilando proyecto en modo Release...
dotnet build -c Release
if errorlevel 1 (
    echo ✗ Error al compilar
    pause
    exit /b 1
)
echo       ✓ Compilación exitosa
echo.

echo [2/4] Copiando archivos...
xcopy "bin\Release\net8.0-windows\*" "%OUTPUT_DIR%\app\" /E /I /Y /Q

REM Excluir archivos que no deben actualizarse
del /q "%OUTPUT_DIR%\app\appsettings.json" 2>nul
del /q "%OUTPUT_DIR%\app\*.db" 2>nul
del /q "%OUTPUT_DIR%\app\*.log" 2>nul

echo       ✓ Archivos copiados
echo.

echo [3/4] Creando archivo ZIP...
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\app\*' -DestinationPath '%OUTPUT_DIR%\ComercioNET_v%VERSION%.zip' -Force"
if errorlevel 1 (
    echo ✗ Error al crear ZIP
    pause
    exit /b 1
)
echo       ✓ ZIP creado
echo.

echo [4/4] Generando version.json...
(
echo {
echo   "Version": "%VERSION%",
echo   "DownloadUrl": "https://tu-servidor.com/updates/comercio-net/ComercioNET_v%VERSION%.zip",
echo   "ReleaseDate": "%date:~-4%-%date:~-7,2%-%date:~-10,2%T12:00:00",
echo   "IsRequired": false,
echo   "FileSize": 0,
echo   "ChangeLog": [
echo     "✅ NUEVO: Describe las nuevas funcionalidades",
echo     "✅ MEJORA: Describe las mejoras realizadas",
echo     "✅ CORRECCIÓN: Describe los bugs corregidos"
echo   ]
echo }
) > "%OUTPUT_DIR%\version.json"

echo       ✓ version.json generado
echo.

echo ╔════════════════════════════════════════════════════════╗
echo ║                                                        ║
echo ║     ✓ EMPAQUETADO COMPLETADO                           ║
echo ║                                                        ║
echo ╚════════════════════════════════════════════════════════╝
echo.
echo Archivos generados en: %OUTPUT_DIR%
echo.
echo PRÓXIMOS PASOS:
echo 1. Editar %OUTPUT_DIR%\version.json con el changelog correcto
echo 2. Subir ComercioNET_v%VERSION%.zip a tu servidor
echo 3. Subir version.json a tu servidor
echo.
pause