using System;
using System.Collections.Generic;

namespace Comercio.NET.Formularios
{
    /// <summary>
    /// Datos de una factura para modificar medios de pago
    /// </summary>
    public class DatosFacturaModificar
    {
        public string NumeroRemito { get; set; }
        public string NumeroFactura { get; set; }
        public string TipoFactura { get; set; }
        public decimal ImporteTotal { get; set; }
        public string FormaPagoActual { get; set; }
        public List<DetallePagoModificar> DetallesPago { get; set; } = new List<DetallePagoModificar>();
    }

    /// <summary>
    /// Detalle de un pago individual
    /// </summary>
    public class DetallePagoModificar
    {
        public string MedioPago { get; set; }
        public decimal Importe { get; set; }
        public string Observaciones { get; set; }
    }

    /// <summary>
    /// Datos de un remito para generar factura AFIP
    /// </summary>
    public class DatosRemitoParaFacturar
    {
        public string NumeroRemito { get; set; }
        public decimal ImporteTotal { get; set; }
        public string FormaPago { get; set; }
        public DateTime Fecha { get; set; }
    }
}