namespace Comercio.NET.Mobile.Server.Models
{
    // ??? Request: agregar un ítem al ticket en curso ???????????????????????????
    public class AgregarItemRequest
    {
        public string NroRemito   { get; set; } = "";
        public string Codigo      { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public int    Cantidad    { get; set; } = 1;
        public decimal Precio     { get; set; }
        public decimal Costo      { get; set; }
        public string Rubro       { get; set; } = "";
        public string Marca       { get; set; } = "";
        public string Proveedor   { get; set; } = "";
        public decimal PorcentajeIva { get; set; }
        public bool   EsCtaCte    { get; set; }
        public string NombreCtaCte { get; set; } = "";
        public bool   EditarPrecio { get; set; }
        public bool   PermiteAcumular { get; set; } = true;
    }

    // ??? Request: confirmar y cerrar el ticket (cobro) ?????????????????????????
    public class ConfirmarVentaRequest
    {
        public string   NroRemito    { get; set; } = "";
        public string   FormaPago    { get; set; } = "Efectivo";
        public string   TipoFactura  { get; set; } = "Ticket";
        public int      NumeroCajero { get; set; }
        public string   UsuarioVenta { get; set; } = "";
        public bool     EsCtaCte     { get; set; }
        public string   NombreCtaCte { get; set; } = "";
        public string   CuitCliente  { get; set; } = "";
        public decimal  PorcentajeDescuento { get; set; }
        public decimal  ImporteDescuento { get; set; }
        public List<DetallePagoDto> Pagos { get; set; } = new();
    }

    public class DetallePagoDto
    {
        public string  MedioPago    { get; set; } = "";
        public decimal Importe      { get; set; }
        public string  Observaciones { get; set; } = "";
    }

    // ??? Response: resultado de confirmar venta ????????????????????????????????
    public class ConfirmarVentaResponse
    {
        public bool   Ok          { get; set; }
        public long   IdFactura   { get; set; }
        public string NroRemito   { get; set; } = "";
        public string Mensaje     { get; set; } = "";
        public string? CAE        { get; set; }
        public string? NroFactura { get; set; }
    }

    // ??? Response: ítem del ticket actual ?????????????????????????????????????
    public class ItemTicketDto
    {
        public long    Id          { get; set; }
        public string  Codigo      { get; set; } = "";
        public string  Descripcion { get; set; } = "";
        public int     Cantidad    { get; set; }
        public decimal Precio      { get; set; }
        public decimal Total       { get; set; }
    }

    // ??? Response: estado del ticket actual ???????????????????????????????????
    public class TicketActivoDto
    {
        public string           NroRemito { get; set; } = "";
        public List<ItemTicketDto> Items  { get; set; } = new();
        public decimal          Total     { get; set; }
    }
    // ??? Configuración de caja (formas de pago, factura electrónica) ??????????
    public class CajaConfigDto
    {
        /// <summary>Formas de pago que requieren generar factura electrónica automáticamente.</summary>
        public List<string> FormasPagoConFactura { get; set; } = new();
        /// <summary>Todas las formas de pago disponibles.</summary>
        public List<string> FormasPagoDisponibles { get; set; } = new();
        /// <summary>Número de cajero del usuario actual (leído de BD si está configurado).</summary>
        public int NumeroCajero { get; set; }
        /// <summary>Condición IVA del emisor: Monotributo o ResponsableInscripto.</summary>
        public string CondicionIVA { get; set; } = "Monotributo";
        /// <summary>Si la funcionalidad de Cta.Cte. está habilitada.</summary>
        public bool CtaCteHabilitado { get; set; }
        /// <summary>Lista de clientes con Cta.Cte. disponibles.</summary>
        public List<string> CtaCteClientes { get; set; } = new();
        /// <summary>Opciones de descuento disponibles (porcentajes).</summary>
        public List<decimal> DescuentoOpciones { get; set; } = new();
        /// <summary>Si el descuento se restringe a ciertos métodos de pago.</summary>
        public bool DescuentoRestringirPorPago { get; set; }
        /// <summary>Métodos de pago que permiten aplicar descuento.</summary>
        public List<string> DescuentoMetodosPagoPermitidos { get; set; } = new();
    }
}
