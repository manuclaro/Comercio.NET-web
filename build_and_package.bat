@echo off
chcp 65001 >nul
title Empaquetar Comercio .NET para Actualizacion

echo ========================================================
echo.
echo     EMPAQUETADOR DE ACTUALIZACIONES
echo     Comercio .NET
echo.
echo ========================================================
echo.

REM Solicitar version
set /p VERSION="Ingrese el numero de version (ej: 1.3.0): "

if "%VERSION%"=="" (
    echo Error: Debe ingresar un numero de version
    pause
    exit /b 1
)

echo.
echo Version: %VERSION%
echo.

REM Crear carpeta de salida
set OUTPUT_DIR=Releases\v%VERSION%
if exist "%OUTPUT_DIR%" (
    echo La carpeta %OUTPUT_DIR% ya existe.
    
    REM ✅ ARREGLO: Mejorar la comparación
    :PREGUNTAR_SOBRESCRIBIR
    set /p OVERWRITE="Desea sobrescribirla? (S/N): "
    
    REM Convertir a mayúscula para comparar
    if /i "%OVERWRITE%"=="S" (
        echo Sobrescribiendo carpeta existente...
        rd /s /q "%OUTPUT_DIR%"
        goto CONTINUAR
    )
    
    if /i "%OVERWRITE%"=="N" (
        echo Operacion cancelada por el usuario
        pause
        exit /b 0
    )
    
    REM Si no es ni S ni N, preguntar de nuevo
    echo Por favor, ingrese S o N
    goto PREGUNTAR_SOBRESCRIBIR
)

:CONTINUAR
mkdir "%OUTPUT_DIR%"

echo.
echo [1/4] Compilando proyecto en modo Release...
dotnet build "Comercio.NET.sln" -c Release
if errorlevel 1 (
    echo X Error al compilar
    pause
    exit /b 1
)
echo       OK Compilacion exitosa
echo.

echo [2/4] Copiando archivos...
xcopy "bin\Release\net8.0-windows\*" "%OUTPUT_DIR%\app\" /E /I /Y /Q

REM Excluir archivos que no deben actualizarse
del /q "%OUTPUT_DIR%\app\appsettings.json" 2>nul
del /q "%OUTPUT_DIR%\app\*.db" 2>nul
del /q "%OUTPUT_DIR%\app\*.log" 2>nul

echo       OK Archivos copiados
echo.

echo [3/4] Creando archivo ZIP...
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\app\*' -DestinationPath '%OUTPUT_DIR%\ComercioNET_v%VERSION%.zip' -Force"
if errorlevel 1 (
    echo X Error al crear ZIP
    pause
    exit /b 1
)
echo       OK ZIP creado
echo.

echo [4/4] Generando version.json...
(
echo {
echo   "Version": "%VERSION%",
echo   "DownloadUrl": "https://github.com/manuclaro/Comercio.NET-web/releases/download/v%VERSION%/ComercioNET_v%VERSION%.zip",
echo   "ReleaseDate": "%date:~-4%-%date:~-7,2%-%date:~-10,2%T12:00:00",
echo   "IsRequired": false,
echo   "FileSize": 0,
echo   "ChangeLog": [
echo     "NUEVO: Describe las nuevas funcionalidades",
echo     "MEJORA: Describe las mejoras realizadas",
echo     "CORRECCION: Describe los bugs corregidos"
echo   ]
echo }
) > "%OUTPUT_DIR%\version.json"

echo       OK version.json generado
echo.

echo ========================================================
echo.
echo     EMPAQUETADO COMPLETADO
echo.
echo ========================================================
echo.
echo Archivos generados en: %OUTPUT_DIR%
echo.
echo PROXIMOS PASOS:
echo 1. Editar %OUTPUT_DIR%\version.json con el changelog correcto
echo 2. Subir ComercioNET_v%VERSION%.zip a GitHub Releases
echo 3. Subir version.json a GitHub Releases
echo.
pause