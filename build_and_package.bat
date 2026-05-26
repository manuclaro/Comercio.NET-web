@echo off
chcp 65001 >nul
title Empaquetar Comercio .NET para Actualizacion

echo ========================================================
echo.
echo     EMPAQUETADOR DE ACTUALIZACIONES
echo     Comercio .NET  (PostgreSQL)
echo.
echo ========================================================
echo.

REM ---------------------------------------------------------------------------
REM Configuracion de PostgreSQL (ajustar si es necesario)
REM ---------------------------------------------------------------------------
set PG_BIN=C:\Program Files\PostgreSQL\18\bin
set PG_USER=postgres
set PG_PORT=5433
set PG_DB=comercio
REM La password se lee de la variable de entorno PGPASSWORD para no exponerla
REM Si PGPASSWORD no esta definida, pg_dump pedira la password interactivamente.
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
echo [1/5] Compilando proyecto en modo Release...
dotnet build "Comercio.NET.sln" -c Release
if errorlevel 1 (
    echo X Error al compilar
    pause
    exit /b 1
)
echo       OK Compilacion exitosa
echo.

echo [2/5] Copiando archivos...
xcopy "bin\Release\net8.0-windows\*" "%OUTPUT_DIR%\app\" /E /I /Y /Q

REM Excluir archivos que no deben distribuirse
del /q "%OUTPUT_DIR%\app\appsettings.json" 2>nul
del /q "%OUTPUT_DIR%\app\*.db" 2>nul
del /q "%OUTPUT_DIR%\app\*.log" 2>nul

REM Copiar scripts de base de datos PostgreSQL
if not exist "%OUTPUT_DIR%\app\database" mkdir "%OUTPUT_DIR%\app\database"
if exist "database\init_comercio_pg.sql" (
    copy /Y "database\init_comercio_pg.sql" "%OUTPUT_DIR%\app\database\" >nul
    echo       OK init_comercio_pg.sql incluido
)

echo       OK Archivos copiados
echo.

echo [3/5] Generando dump de PostgreSQL...
if not exist "%PG_BIN%\pg_dump.exe" (
    echo    !! pg_dump no encontrado en: %PG_BIN%
    echo    !! Ajuste la variable PG_BIN al inicio del script.
    echo    !! El dump NO sera incluido en el paquete.
    echo    !! Los clientes usaran init_comercio_pg.sql como fallback.
    goto SKIP_DUMP
)

REM Generar dump en formato SQL plano (mas portable entre versiones)
echo    >> Generando comercio_inicial.sql (formato SQL plano)...
"%PG_BIN%\pg_dump.exe" -U %PG_USER% -p %PG_PORT% -d %PG_DB% ^
    --format=plain --no-owner --no-acl ^
    --file="%OUTPUT_DIR%\app\database\comercio_inicial.sql"

if errorlevel 1 (
    echo    !! Error generando el dump SQL plano.
    echo    !! Verifique que PostgreSQL este corriendo y que "%PG_DB%" exista.
    echo    !! Los clientes usaran init_comercio_pg.sql como fallback.
) else (
    echo       OK Dump SQL plano generado: database\comercio_inicial.sql
)

:SKIP_DUMP
echo.

echo [4/5] Creando archivo ZIP...
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\app\*' -DestinationPath '%OUTPUT_DIR%\ComercioNET_v%VERSION%.zip' -Force"
if errorlevel 1 (
    echo X Error al crear ZIP
    pause
    exit /b 1
)
echo       OK ZIP creado
echo.

echo [5/5] Generando version.json...
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
echo CONTENIDO DEL ZIP:
echo   - Ejecutable y DLLs de la aplicacion
echo   - database\comercio_inicial.sql   (SQL plano, compatible entre versiones PG)
echo   - database\init_comercio_pg.sql   (DDL fallback)
echo.
pause