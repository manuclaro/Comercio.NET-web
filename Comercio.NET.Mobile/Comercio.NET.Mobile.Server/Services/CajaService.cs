using Comercio.NET.Mobile.Server.Models;
using System.Text.Json;

namespace Comercio.NET.Mobile.Server.Services
{
    public class CajaService : ICajaService
    {
        private readonly DbService _db;
        private readonly ILogger<CajaService> _logger;
        private readonly AfipService _afipService;

        public CajaService(DbService db, ILogger<CajaService> logger, AfipService afipService)
        {
            _db     = db;
            _logger = logger;
            _afipService = afipService;
        }

        // ?? Helpers (alias cortos) ?????????????????????????????????????????????
        private Task ExecuteAsync(string sql, Dictionary<string, object?> p) => _db.ExecuteAsync(sql, p);
        private Task<T?> ScalarAsync<T>(string sql, Dictionary<string, object?> p) => _db.ScalarAsync<T>(sql, p);
        private Task<List<List<JsonElement>>> QueryAsync(string sql, Dictionary<string, object?> p) => _db.QueryAsync(sql, p);

        // ?? Generar número de remito ????????????????????????????????????????????

        /// <summary>Convierte nroRemito string (ej: "R-111946" o "111946") a int para la BD.</summary>
        private static int ParseNroRemito(string nroRemito)
        {
            var num = nroRemito.StartsWith("R-", StringComparison.OrdinalIgnoreCase)
                ? nroRemito.Substring(2)
                : nroRemito;
            return int.TryParse(num, out var result) ? result : 0;
        }

        public async Task<string> GenerarNroRemitoAsync()
        {
            try
            {
                // Incrementar y obtener el número de remito del contador global
                await ExecuteAsync("UPDATE numeroremito SET nroremito = nroremito + 1", new());
                var nro = await ScalarAsync<int>("SELECT nroremito FROM numeroremito", new());
                return $"R-{nro}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GenerarNroRemitoAsync con tabla numeroremito, intentando MAX");
                try
                {
                    // Fallback: obtener MAX de ventas + 1
                    var max = await ScalarAsync<long>("SELECT COALESCE(MAX(nrofactura), 0) FROM ventas", new());
                    return $"R-{max + 1}";
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Error en fallback GenerarNroRemitoAsync");
                    return $"R-{DateTime.Now.Ticks % 1000000}";
                }
            }
        }

        // ?? Obtener ticket actual ???????????????????????????????????????????????

        public async Task<TicketActivoDto> GetTicketAsync(string nroRemito)
        {
            var sql = @"
                SELECT id, codigo, descripcion, cantidad, precio, total
                FROM ventas
                WHERE nrofactura = @nroRemito
                ORDER BY id DESC";

            var rows = await QueryAsync(sql, new() { { "@nroRemito", ParseNroRemito(nroRemito) } });

            var items = rows.Select(r => new ItemTicketDto
            {
                Id          = r[0].GetInt64(),
                Codigo      = r[1].GetString() ?? "",
                Descripcion = r[2].GetString() ?? "",
                Cantidad    = r[3].GetInt32(),
                Precio      = r[4].GetDecimal(),
                Total       = r[5].GetDecimal(),
            }).ToList();

            return new TicketActivoDto
            {
                NroRemito = nroRemito,
                Items     = items,
                Total     = items.Sum(i => i.Total)
            };
        }

        // ?? Agregar ítem ????????????????????????????????????????????????????????

        public async Task<ItemTicketDto> AgregarItemAsync(AgregarItemRequest req)
        {
            // Verificar si el producto ya está en el ticket
            var sqlCheck = @"
                SELECT id, cantidad FROM ventas
                WHERE nrofactura = @nroRemito AND codigo = @codigo";

            var existing = await QueryAsync(sqlCheck, new()
            {
                { "@nroRemito", ParseNroRemito(req.NroRemito) },
                { "@codigo",    req.Codigo }
            });

            decimal ivaCalculado = Math.Round(
                req.Precio * req.Cantidad * (req.PorcentajeIva / (100m + req.PorcentajeIva)), 2);
            decimal total = req.Precio * req.Cantidad;

            // Acumular cantidad solo si PermiteAcumular=true
            // Si PermiteAcumular=false ? siempre nueva línea (aunque el código exista)
            if (existing.Count > 0 && req.PermiteAcumular)
            {
                // Acumular cantidad en registro existente
                long   idExistente      = existing[0][0].GetInt64();
                int    cantidadAnterior = existing[0][1].GetInt32();
                int    nuevaCantidad    = cantidadAnterior + req.Cantidad;
                decimal nuevoTotal      = req.Precio * nuevaCantidad;
                decimal nuevoIva        = Math.Round(nuevoTotal * (req.PorcentajeIva / (100m + req.PorcentajeIva)), 2);

                var sqlUpdate = @"
                    UPDATE ventas SET
                        cantidad     = @cantidad,
                        total        = @total,
                        ivacalculado = @iva
                    WHERE id = @id";

                await ExecuteAsync(sqlUpdate, new()
                {
                    { "@cantidad", nuevaCantidad },
                    { "@total",    nuevoTotal },
                    { "@iva",      nuevoIva },
                    { "@id",       idExistente }
                });

                return new ItemTicketDto
                {
                    Id          = idExistente,
                    Codigo      = req.Codigo,
                    Descripcion = req.Descripcion,
                    Cantidad    = nuevaCantidad,
                    Precio      = req.Precio,
                    Total       = nuevoTotal
                };
            }

            // Insertar nuevo ítem
            var esCtaCteVal = "@esCtaCte";
            var esOfertaVal = _db.UsaPostgres ? "FALSE" : "0";

            var sqlInsert = $@"
                INSERT INTO ventas
                    (nrofactura, codigo, descripcion, cantidad, precio, total,
                     ivacalculado, porcentajeiva, rubro, marca, proveedor, costo,
                     fecha, hora, esctacte, nombrectacte,
                     idoferta, nombreoferta, preciooriginal, precioconoferta,
                     descuentoaplicado, esoferta)
                VALUES
                    (@nroRemito, @codigo, @descripcion, @cantidad, @precio, @total,
                     @iva, @porcentajeIva, @rubro, @marca, @proveedor, @costo,
                     @fecha, @hora, {esCtaCteVal}, @nombreCtaCte,
                     NULL, NULL, NULL, NULL, NULL, {esOfertaVal})";

            await ExecuteAsync(sqlInsert, new()
            {
                { "@nroRemito",    ParseNroRemito(req.NroRemito) },
                { "@codigo",       req.Codigo },
                { "@descripcion",  req.Descripcion },
                { "@cantidad",     req.Cantidad },
                { "@precio",       req.Precio },
                { "@total",        total },
                { "@iva",          ivaCalculado },
                { "@porcentajeIva",req.PorcentajeIva },
                { "@rubro",        req.Rubro },
                { "@marca",        req.Marca },
                { "@proveedor",    req.Proveedor },
                { "@costo",        req.Costo },
                { "@fecha",        DateTime.Now.Date },
                { "@hora",         DateTime.Now },
                { "@esCtaCte",     req.EsCtaCte },
                { "@nombreCtaCte", req.EsCtaCte ? req.NombreCtaCte : null }
            });

            // Obtener el id generado
            var idSql  = "SELECT MAX(id) FROM ventas WHERE nrofactura = @nroRemito AND codigo = @codigo";
            var newId  = await ScalarAsync<long>(idSql, new()
            {
                { "@nroRemito", ParseNroRemito(req.NroRemito) },
                { "@codigo",    req.Codigo }
            });

            return new ItemTicketDto
            {
                Id          = newId,
                Codigo      = req.Codigo,
                Descripcion = req.Descripcion,
                Cantidad    = req.Cantidad,
                Precio      = req.Precio,
                Total       = total
            };
        }

        // ?? Actualizar cantidad de un ítem ??????????????????????????????????????

        public async Task<ItemTicketDto> ActualizarCantidadAsync(long idVenta, int nuevaCantidad)
        {
            // Obtener datos actuales del ítem
            var rows = await QueryAsync(
                "SELECT codigo, descripcion, precio FROM ventas WHERE id = @id",
                new() { { "@id", idVenta } });

            if (rows.Count == 0) throw new Exception($"Ítem {idVenta} no encontrado en el ticket");

            var codigo      = rows[0][0].GetString() ?? "";
            var descripcion = rows[0][1].GetString() ?? "";
            var precio      = rows[0][2].GetDecimal();
            var nuevoTotal  = precio * nuevaCantidad;
            var nuevoIva    = Math.Round(nuevoTotal * (21m / 121m), 2); // IVA 21% incluido por defecto

            await ExecuteAsync(@"
                UPDATE ventas SET
                    cantidad     = @cantidad,
                    total        = @total,
                    ivacalculado = @iva
                WHERE id = @id",
                new()
                {
                    { "@cantidad", nuevaCantidad },
                    { "@total",    nuevoTotal },
                    { "@iva",      nuevoIva },
                    { "@id",       idVenta }
                });

            return new ItemTicketDto
            {
                Id          = idVenta,
                Codigo      = codigo,
                Descripcion = descripcion,
                Cantidad    = nuevaCantidad,
                Precio      = precio,
                Total       = nuevoTotal
            };
        }

        // ?? Eliminar ítem ???????????????????????????????????????????????????????

        public async Task EliminarItemAsync(long idVenta, string? usuario = null, int? numeroCajero = null, string? motivo = null)
        {
            // Obtener datos del item antes de eliminar para registrar auditoría
            var rows = await QueryAsync(
                "SELECT codigo, descripcion, precio, cantidad, nrofactura FROM ventas WHERE id = @id",
                new() { { "@id", idVenta } });

            if (rows.Count > 0)
            {
                var row = rows[0];
                var codigo = row[0].ValueKind == System.Text.Json.JsonValueKind.Null ? "" : row[0].GetString() ?? "";
                var descripcion = row[1].ValueKind == System.Text.Json.JsonValueKind.Null ? "" : row[1].GetString() ?? "";
                var precio = row[2].ValueKind == System.Text.Json.JsonValueKind.Number ? row[2].GetDecimal() : 0m;
                var cantidad = row[3].ValueKind == System.Text.Json.JsonValueKind.Number ? row[3].GetInt32() : 0;
                var nroFactura = row[4].ValueKind == System.Text.Json.JsonValueKind.Number ? row[4].GetInt32() : 0;

                await RegistrarAuditoriaEliminacionAsync(
                    codigo, descripcion, precio, cantidad, nroFactura,
                    usuario ?? "web", numeroCajero ?? 0, motivo ?? "ELIMINACIÓN PRODUCTO - WEB",
                    true, cantidad);
            }

            await ExecuteAsync(
                "DELETE FROM ventas WHERE id = @id",
                new() { { "@id", idVenta } });
        }

        // ? Cancelar ticket completo ????????????????????????????????????????

        public async Task CancelarTicketAsync(string nroRemito, string? usuario = null, int? numeroCajero = null)
        {
            var nroRemitoInt = ParseNroRemito(nroRemito);

            // Obtener todos los items para registrar auditoría
            var rows = await QueryAsync(
                "SELECT codigo, descripcion, precio, cantidad FROM ventas WHERE nrofactura = @nroRemito",
                new() { { "@nroRemito", nroRemitoInt } });

            foreach (var row in rows)
            {
                var codigo = row[0].ValueKind == System.Text.Json.JsonValueKind.Null ? "" : row[0].GetString() ?? "";
                var descripcion = row[1].ValueKind == System.Text.Json.JsonValueKind.Null ? "" : row[1].GetString() ?? "";
                var precio = row[2].ValueKind == System.Text.Json.JsonValueKind.Number ? row[2].GetDecimal() : 0m;
                var cantidad = row[3].ValueKind == System.Text.Json.JsonValueKind.Number ? row[3].GetInt32() : 0;

                await RegistrarAuditoriaEliminacionAsync(
                    codigo, descripcion, precio, cantidad, nroRemitoInt,
                    usuario ?? "web", numeroCajero ?? 0, "ANULACIÓN FACTURA COMPLETA - WEB",
                    true, cantidad);
            }

            await ExecuteAsync(
                "DELETE FROM ventas WHERE nrofactura = @nroRemito",
                new() { { "@nroRemito", nroRemitoInt } });

            // También eliminar la factura si existe
            await ExecuteAsync(
                "DELETE FROM facturas WHERE numeroremito = @nroRemito",
                new() { { "@nroRemito", nroRemitoInt } });
        }

        private async Task RegistrarAuditoriaEliminacionAsync(
            string codigo, string descripcion, decimal precio, int cantidad,
            int numeroFactura, string usuario, int numeroCajero, string motivo,
            bool esEliminacionCompleta, int cantidadOriginal)
        {
            var esBitTrue = _db.UsaPostgres ? "TRUE" : "1";
            var esBitFalse = _db.UsaPostgres ? "FALSE" : "0";

            var sql = $@"
                INSERT INTO auditoriaproductoseliminados 
                    (codigoproducto, descripcionproducto, preciounitario, cantidad, 
                     totaleliminado, numerofactura, fechahoraventaoriginal, fechaeliminacion, 
                     motivoeliminacion, esctacte, usuarioeliminacion, 
                     numerocajero, nombreequipo, eseliminacioncompleta, cantidadoriginal)
                VALUES 
                    (@codigo, @descripcion, @precio, @cantidad,
                     @total, @numFactura, @fechaHora, @fechaElim,
                     @motivo, {esBitFalse}, @usuario,
                     @cajero, @equipo, {(esEliminacionCompleta ? esBitTrue : esBitFalse)}, @cantOriginal)";

            await ExecuteAsync(sql, new()
            {
                { "@codigo", codigo },
                { "@descripcion", descripcion },
                { "@precio", precio },
                { "@cantidad", cantidad },
                { "@total", precio * cantidad },
                { "@numFactura", numeroFactura },
                { "@fechaHora", DateTime.Now },
                { "@fechaElim", DateTime.Now },
                { "@motivo", motivo },
                { "@usuario", usuario },
                { "@cajero", numeroCajero },
                { "@equipo", "Web" },
                { "@cantOriginal", cantidadOriginal }
            });
        }

        // ?? Confirmar venta (cerrar ticket y registrar factura) ?????????????????

        public async Task<ConfirmarVentaResponse> ConfirmarVentaAsync(ConfirmarVentaRequest req)
        {
            // 1. Calcular total del ticket
            var totalSql = _db.UsaPostgres
                ? "SELECT COALESCE(SUM(total), 0) FROM ventas WHERE nrofactura = @nroRemito"
                : "SELECT ISNULL(SUM(total), 0) FROM ventas WHERE nrofactura = @nroRemito";
            var importeTotal = await ScalarAsync<decimal>(totalSql, new() { { "@nroRemito", ParseNroRemito(req.NroRemito) } });

            // Calcular IVA total desde los items (porcentajeiva sobre precio * cantidad)
            var ivaSql = _db.UsaPostgres
                ? "SELECT COALESCE(SUM(total * porcentajeiva / 100.0), 0) FROM ventas WHERE nrofactura = @nroRemito AND porcentajeiva > 0"
                : "SELECT ISNULL(SUM(total * porcentajeiva / 100.0), 0) FROM ventas WHERE nrofactura = @nroRemito AND porcentajeiva > 0";
            var ivaTotal = await ScalarAsync<decimal>(ivaSql, new() { { "@nroRemito", ParseNroRemito(req.NroRemito) } });

            // 2. Insertar en facturas y obtener el id generado.
            string insertFacturaSql = _db.UsaPostgres
                ? @"INSERT INTO facturas
                        (numeroremito, formadepago, tipofactura, cajero, usuarioventa,
                         fecha, hora, importetotal, importefinal, esctacte, ctactenombre,
                         cuitcliente, iva, porcentajedescuento, importedescuento)
                    VALUES
                        (@nroRemito, @formaPago, @tipoFactura, @cajero, @usuario,
                         @fecha, @hora, @total, @importeFinal, @esCtaCte, @nombreCtaCte,
                         @cuitCliente, @iva, @porcDesc, @impDesc)
                    RETURNING idfactura"
                : @"INSERT INTO facturas
                        (numeroremito, formadepago, tipofactura, cajero, usuarioventa,
                         fecha, hora, importetotal, importefinal, esctacte, ctactenombre,
                         cuitcliente, iva, porcentajedescuento, importedescuento)
                    OUTPUT INSERTED.idfactura
                    VALUES
                        (@nroRemito, @formaPago, @tipoFactura, @cajero, @usuario,
                         @fecha, @hora, @total, @importeFinal, @esCtaCte, @nombreCtaCte,
                         @cuitCliente, @iva, @porcDesc, @impDesc)";

            var facturaParams = new Dictionary<string, object?>
            {
                { "@nroRemito",    ParseNroRemito(req.NroRemito) },
                { "@formaPago",    req.FormaPago },
                { "@tipoFactura",  req.TipoFactura },
                { "@cajero",       req.NumeroCajero.ToString() },
                { "@usuario",      req.UsuarioVenta },
                { "@fecha",        DateTime.Now.Date },
                { "@hora",         DateTime.Now },
                { "@total",        importeTotal },
                { "@importeFinal", req.PorcentajeDescuento > 0 ? importeTotal - req.ImporteDescuento : importeTotal },
                { "@esCtaCte",     req.EsCtaCte },
                { "@nombreCtaCte", req.EsCtaCte ? req.NombreCtaCte : null },
                { "@cuitCliente",  string.IsNullOrWhiteSpace(req.CuitCliente) ? (object?)null : req.CuitCliente },
                { "@iva",          ivaTotal },
                { "@porcDesc",     req.PorcentajeDescuento },
                { "@impDesc",      req.ImporteDescuento }
            };

            var idFactura = await ScalarAsync<long>(insertFacturaSql, facturaParams);

            // 3. Insertar detalles de pago
            var insertDetalleSql = @"
                INSERT INTO detallespagofactura
                    (idfactura, mediopago, importe, observaciones, fechapago, usuario, numeroremito)
                VALUES
                    (@idFactura, @medioPago, @importe, @obs, @fecha, @usuario, @nroRemito)";

            if (req.Pagos != null && req.Pagos.Count > 0)
            {
                foreach (var pago in req.Pagos)
                {
                    await ExecuteAsync(insertDetalleSql, new()
                    {
                        { "@idFactura",  idFactura },
                        { "@medioPago",  pago.MedioPago },
                        { "@importe",    pago.Importe },
                        { "@obs",        string.IsNullOrWhiteSpace(pago.Observaciones) ? null : pago.Observaciones },
                        { "@fecha",      DateTime.Now },
                        { "@usuario",    req.UsuarioVenta },
                        { "@nroRemito",  ParseNroRemito(req.NroRemito) }
                    });
                }
            }
            else
            {
                await ExecuteAsync(insertDetalleSql, new()
                {
                    { "@idFactura",  idFactura },
                    { "@medioPago",  req.FormaPago },
                    { "@importe",    req.PorcentajeDescuento > 0 ? importeTotal - req.ImporteDescuento : importeTotal },
                    { "@obs",        null },
                    { "@fecha",      DateTime.Now },
                    { "@usuario",    req.UsuarioVenta },
                    { "@nroRemito",  ParseNroRemito(req.NroRemito) }
                });
            }

            // 4. Descontar stock de los productos vendidos
            var itemsSql = "SELECT codigo, cantidad FROM ventas WHERE nrofactura = @nroRemito";
            var items = await QueryAsync(itemsSql, new() { { "@nroRemito", ParseNroRemito(req.NroRemito) } });

            foreach (var item in items)
            {
                var codigo   = item[0].GetString();
                var cantidad = item[1].GetInt32();
                await ExecuteAsync(
                    "UPDATE productos SET cantidad = cantidad - @cantidad WHERE codigo = @codigo",
                    new() { { "@cantidad", cantidad }, { "@codigo", codigo } });
            }

            // 5. Si es factura electronica, solicitar CAE a AFIP
            string? caeObtenido = null;
            string? nroFacturaAfip = null;
            string? errorAfip = null;
            if (req.TipoFactura.StartsWith("Factura", StringComparison.OrdinalIgnoreCase))
            {
                string tipoLetra = req.TipoFactura.Replace("Factura", ""); // "A", "B", "C"
                var importeFinal = req.PorcentajeDescuento > 0 ? importeTotal - req.ImporteDescuento : importeTotal;
                _logger.LogInformation("Solicitando factura electronica tipo {Tipo} por {Monto} - Remito: {Remito}", tipoLetra, importeFinal, req.NroRemito);

                try
                {
                    var resultadoAfip = await _afipService.GenerarFacturaElectronicaAsync(tipoLetra, importeFinal, req.CuitCliente);

                    if (resultadoAfip.Exito)
                    {
                        caeObtenido = resultadoAfip.CAE;
                        nroFacturaAfip = resultadoAfip.NumeroFactura;

                        _logger.LogInformation("CAE obtenido: {CAE}, Factura: {Nro}, actualizando remito {Remito}", caeObtenido, nroFacturaAfip, ParseNroRemito(req.NroRemito));

                        // Actualizar la factura con CAE y numero usando numeroremito (mas confiable)
                        await ExecuteAsync(@"
                            UPDATE facturas SET 
                                nrofactura = @nroFactura,
                                caenumero = @cae,
                                caevencimiento = @caeVenc
                            WHERE numeroremito = @nroRemito",
                            new()
                            {
                                { "@nroFactura", nroFacturaAfip },
                                { "@cae", caeObtenido },
                                { "@caeVenc", resultadoAfip.VencimientoCAE },
                                { "@nroRemito", ParseNroRemito(req.NroRemito) }
                            });
                    }
                    else
                    {
                        errorAfip = resultadoAfip.Error;
                        _logger.LogWarning("No se pudo obtener CAE de AFIP: {Error}", errorAfip);
                    }
                }
                catch (Exception exAfip)
                {
                    errorAfip = exAfip.Message;
                    _logger.LogError(exAfip, "Error al comunicarse con AFIP para remito {Remito}", req.NroRemito);
                }

                // Si AFIP falla, revertir la venta para que el usuario pueda corregir
                if (errorAfip != null)
                {
                    _logger.LogWarning("Revirtiendo venta por error AFIP - remito {Remito}", req.NroRemito);

                    // Restaurar stock
                    foreach (var item in items)
                    {
                        var codigo = item[0].GetString();
                        var cantidad = item[1].GetInt32();
                        await ExecuteAsync(
                            "UPDATE productos SET cantidad = cantidad + @cantidad WHERE codigo = @codigo",
                            new() { { "@cantidad", cantidad }, { "@codigo", codigo } });
                    }

                    // Eliminar detalle de pago y factura
                    await ExecuteAsync("DELETE FROM detallespagofactura WHERE idfactura = @id", new() { { "@id", idFactura } });
                    await ExecuteAsync("DELETE FROM facturas WHERE idfactura = @id", new() { { "@id", idFactura } });

                    return new ConfirmarVentaResponse
                    {
                        Ok = false,
                        IdFactura = 0,
                        NroRemito = req.NroRemito,
                        Mensaje = $"Error al generar factura electronica: {errorAfip}. Verifique el CUIT e intente nuevamente.",
                        CAE = null,
                        NroFactura = null
                    };
                }
            }

            _logger.LogInformation("Venta confirmada: remito={NroRemito}, idFactura={IdFactura}, CAE={CAE}", req.NroRemito, idFactura, caeObtenido ?? "N/A");

            string mensaje;
            if (caeObtenido != null)
                mensaje = $"Factura {nroFacturaAfip} generada - CAE: {caeObtenido}";
            else if (errorAfip != null)
                mensaje = $"Venta registrada pero error AFIP: {errorAfip}";
            else
                mensaje = "Venta registrada correctamente";

            return new ConfirmarVentaResponse
            {
                Ok        = true,
                IdFactura = idFactura,
                NroRemito = req.NroRemito,
                Mensaje   = mensaje,
                CAE = caeObtenido,
                NroFactura = nroFacturaAfip
            };
        }
    }
}
