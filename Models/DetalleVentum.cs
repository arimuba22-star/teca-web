using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace SWebEnergia.Models
{
    public partial class DetalleVentum
    {
        // Hacer IdDetalle anulable
        public int? IdDetalle { get; set; }

        // Hacer IdVenta anulable
        public int? IdVenta { get; set; }

        public int IdProducto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal? Importe { get; set; }

        [ValidateNever]
        [JsonIgnore]
        public virtual Producto IdProductoNavigation { get; set; } = null!;

        [ValidateNever]
        [JsonIgnore]
        public virtual Venta IdVentaNavigation { get; set; } = null!;
    }
}