// Venta.cs

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SWebEnergia.Models
{
    public partial class Venta
    {
        public int IdVenta { get; set; }
        public int IdCliente { get; set; }
        public int? IdMantenimiento { get; set; }
        public string? TipoComprobante { get; set; }
        public string? NumeroComprobante { get; set; } 

        public decimal? Subtotal { get; set; }
        public decimal? Impuestos { get; set; }
        public decimal? Total { get; set; }

        public DateTime? Fecha { get; set; } = DateTime.Now;

        // Relaciones
        [ForeignKey("IdCliente")]
        [JsonIgnore]
        public virtual Cliente? IdClienteNavigation { get; set; }

        [ForeignKey("IdMantenimiento")]
        [JsonIgnore]
        public virtual Mantenimiento? IdMantenimientoNavigation { get; set; }

        // Esta propiedad se vincula con la propiedad "detalleVenta" en JSON (minúscula d)
        [JsonPropertyName("detalleVenta")]
        public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();
    }
}
