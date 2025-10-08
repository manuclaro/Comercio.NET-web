@echo off
echo Convertir certificado .crt + .key a .p12
echo.
echo IMPORTANTE: Necesitas tener OpenSSL instalado
echo.
echo Si tienes los archivos:
echo - FETesting.crt
echo - FETesting.key (clave privada)
echo.
echo Ejecuta este comando en la carpeta donde estan los archivos:
echo.
echo openssl pkcs12 -export -out FETesting.p12 -inkey MiClavePrivada.key -in FETesting.crt
echo.
echo Te pedira una contrasena para el archivo .p12 (usa: Micertificado)
echo.
pause