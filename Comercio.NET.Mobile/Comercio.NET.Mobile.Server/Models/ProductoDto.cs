namespace Comercio.NET.Mobile.Server.Models
{
    public class ProductoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string Rubro { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public bool EditarPrecio { get; set; }
        /// <summary>Campo 'porcentaje' de la tabla (margen/porcentaje de ganancia)</summary>
        public int Porcentaje { get; set; }
        /// <summary>Campo 'iva' de la tabla (alícuota IVA: 0, 10.5, 21, 27)</summary>
        public decimal Iva { get; set; }
        public bool Activo { get; set; } = true;
        public string Proveedor { get; set; } = string.Empty;
        public bool PermiteAcumular { get; set; } = true;
    }

    public class ActualizarProductoDto
    {
        public decimal Costo { get; set; }
        public int Porcentaje { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
    }

    public class EditarProductoCompletoDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string Rubro { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int Porcentaje { get; set; }
        public decimal Iva { get; set; }
        public bool EditarPrecio { get; set; }
        public bool PermiteAcumular { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class NuevoProductoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Rubro { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int Porcentaje { get; set; }
        public decimal Iva { get; set; }
        public bool EditarPrecio { get; set; } = true;
        public bool PermiteAcumular { get; set; } = true;
    }
}