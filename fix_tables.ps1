$files = @(
    "Formularios\frmControlFacturas.cs",
    "Formularios\Ventas.cs",
    "Formularios\SeleccionImpresionForm.cs",
    "Formularios\FacturaDirectaAfipForm.cs",
    "Formularios\ProductoFormUnificado.cs",
    "Formularios\AgregarProducto.cs",
    "Formularios\ActualizacionExcelForm.cs",
    "Formularios\ActualizacionRapidaForm.cs",
    "Formularios\ActualizacionMasivaForm.cs",
    "Formularios\CierreTurnoCajeroForm.cs",
    "Formularios\ComprasProveedorForm.cs",
    "Formularios\ProveedoresCtaCteControl.cs",
    "Formularios\ArqueoCajaForm.cs",
    "Formularios\GestionUsuariosForm.cs",
    "Formularios\GestionOfertasForm.cs",
    "Formularios\frmPagosProveedores.cs",
    "PagoProveedorRapidoForm.cs",
    "ControlComprasProveedores.cs",
    "PagosDetalleForm.cs",
    "Services\AuthenticationService.cs",
    "Program.cs"
)

$patterns = @(
    @("FROM Facturas\b",                       "FROM facturas"),
    @("INTO Facturas\b",                       "INTO facturas"),
    @("UPDATE Facturas\b",                     "UPDATE facturas"),
    @("DELETE FROM Facturas\b",                "DELETE FROM facturas"),
    @("INNER JOIN Facturas\b",                 "INNER JOIN facturas"),
    @("LEFT JOIN Facturas\b",                  "LEFT JOIN facturas"),
    @("FROM Ventas\b",                         "FROM ventas"),
    @("INTO Ventas\b",                         "INTO ventas"),
    @("UPDATE Ventas\b",                       "UPDATE ventas"),
    @("DELETE FROM Ventas\b",                  "DELETE FROM ventas"),
    @("INNER JOIN Ventas\b",                   "INNER JOIN ventas"),
    @("LEFT JOIN Ventas\b",                    "LEFT JOIN ventas"),
    @("FROM Productos\b",                      "FROM productos"),
    @("INTO Productos\b",                      "INTO productos"),
    @("UPDATE Productos\b",                    "UPDATE productos"),
    @("DELETE FROM Productos\b",               "DELETE FROM productos"),
    @("INNER JOIN Productos\b",                "INNER JOIN productos"),
    @("FROM Usuarios\b",                       "FROM usuarios"),
    @("INTO Usuarios\b",                       "INTO usuarios"),
    @("UPDATE Usuarios\b",                     "UPDATE usuarios"),
    @("DELETE FROM Usuarios\b",                "DELETE FROM usuarios"),
    @("FROM ComprasProveedores\b",             "FROM comprasproveedores"),
    @("INTO ComprasProveedores\b",             "INTO comprasproveedores"),
    @("UPDATE ComprasProveedores\b",           "UPDATE comprasproveedores"),
    @("INNER JOIN ComprasProveedores\b",       "INNER JOIN comprasproveedores"),
    @("FROM ComprasProveedoresIvaDetalle\b",   "FROM comprasproveedoresivadetalle"),
    @("INTO ComprasProveedoresIvaDetalle\b",   "INTO comprasproveedoresivadetalle"),
    @("FROM PagosProveedores\b",               "FROM pagosproveedores"),
    @("INTO PagosProveedores\b",               "INTO pagosproveedores"),
    @("FROM ProveedoresCtaCte\b",              "FROM proveedoresctacte"),
    @("INTO ProveedoresCtaCte\b",              "INTO proveedoresctacte"),
    @("UPDATE ProveedoresCtaCte\b",            "UPDATE proveedoresctacte"),
    @("FROM Proveedores\b",                    "FROM proveedores"),
    @("FROM Gastos\b",                         "FROM gastos"),
    @("INTO Gastos\b",                         "INTO gastos"),
    @("FROM AuditoriaProductos\b",             "FROM auditoriaproductos"),
    @("INTO AuditoriaProductos\b",             "INTO auditoriaproductos"),
    @("FROM AuditoriaProductosEliminados\b",   "FROM auditoriaproductoseliminados"),
    @("INTO AuditoriaProductosEliminados\b",   "INTO auditoriaproductoseliminados"),
    @("FROM TurnosCajero\b",                   "FROM turnoscajero"),
    @("INTO TurnosCajero\b",                   "INTO turnoscajero"),
    @("UPDATE TurnosCajero\b",                 "UPDATE turnoscajero"),
    @("FROM CierreTurnoCajero\b",              "FROM cierreturnocajero"),
    @("INTO CierreTurnoCajero\b",              "INTO cierreturnocajero"),
    @("FROM NumeroRemito\b",                   "FROM numeroremito"),
    @("UPDATE NumeroRemito\b",                 "UPDATE numeroremito"),
    @("FROM OfertasProductos\b",               "FROM ofertasproductos"),
    @("INTO OfertasProductos\b",               "INTO ofertasproductos"),
    @("UPDATE OfertasProductos\b",             "UPDATE ofertasproductos"),
    @("FROM DetalleOfertasProductos\b",        "FROM detalleofertasproductos"),
    @("INTO DetalleOfertasProductos\b",        "INTO detalleofertasproductos"),
    @("FROM ComprasProveedoresCtaCte\b",       "FROM proveedoresctacte"),
    @("UPDATE ComprasProveedoresCtaCte\b",     "UPDATE proveedoresctacte"),
    @("FROM DetallePagoFactura\b",             "FROM detallespagofactura"),
    @("INTO DetallePagoFactura\b",             "INTO detallespagofactura"),
    @("FROM DetallesPagoFactura\b",            "FROM detallespagofactura"),
    @("INTO DetallesPagoFactura\b",            "INTO detallespagofactura"),
    @("FROM Pagos\b",                          "FROM pagos"),
    @("INTO Pagos\b",                          "INTO pagos")
)

$baseDir = $PSScriptRoot

foreach ($relFile in $files) {
    $fullPath = Join-Path $baseDir $relFile
    if (-not (Test-Path $fullPath)) { Write-Output "NOT FOUND: $relFile"; continue }

    $content = [System.IO.File]::ReadAllText($fullPath)
    $original = $content

    foreach ($pair in $patterns) {
        $content = [System.Text.RegularExpressions.Regex]::Replace($content, $pair[0], $pair[1])
    }

    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($fullPath, $content, [System.Text.Encoding]::UTF8)
        Write-Output "UPDATED: $relFile"
    } else {
        Write-Output "no changes: $relFile"
    }
}

Write-Output "=== DONE ==="
