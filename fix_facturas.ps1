$file = "C:\Users\Manuel\source\repos\Comercio .NET\Formularios\frmControlFacturas.cs"
$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$pairs = @(
    @{Old='`"Facturas`"'; New='facturas'},
    @{Old='`"Ventas`"'; New='ventas'},
    @{Old='`"Productos`"'; New='productos'},
    @{Old='`"DetallesPagoFactura`"'; New='detallespagofactura'},
    @{Old='`"NumeroRemito`"'; New='numeroremito'},
    @{Old='`"NroFactura`"'; New='nrofactura'},
    @{Old='`"ImporteFinal`"'; New='importefinal'},
    @{Old='`"PorcentajeDescuento`"'; New='porcentajedescuento'},
    @{Old='`"ImporteDescuento`"'; New='importedescuento'},
    @{Old='`"IVA`"'; New='iva'},
    @{Old='`"Cajero`"'; New='cajero'},
    @{Old='`"Fecha`"'; New='fecha'},
    @{Old='`"Hora`"'; New='hora'},
    @{Old='`"FormadePago`"'; New='formadepago'},
    @{Old='`"TipoFactura`"'; New='tipofactura'},
    @{Old='`"CAENumero`"'; New='caenumero'},
    @{Old='`"CtaCteNombre`"'; New='ctactombre'},
    @{Old='`"esCtaCte`"'; New='esctacte'},
    @{Old='`"IdFactura`"'; New='idfactura'}
)

foreach ($pair in $pairs) {
    $old = $pair.Old
    $new = $pair.New
    $count = ([regex]::Matches($content, [regex]::Escape($old))).Count
    if ($count -gt 0) {
        Write-Host "Replacing $count x '$old' -> '$new'"
        $content = $content.Replace($old, $new)
    }
}

[System.IO.File]::WriteAllText($file, $content, [System.Text.Encoding]::UTF8)
Write-Host "DONE"
