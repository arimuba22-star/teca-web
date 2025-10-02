using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWebEnergia.Models;

public partial class Comprobante
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdComprobante { get; set; }

    public int IdCliente { get; set; }

    public string Tipo { get; set; } = null!;

    public DateTime? Fecha { get; set; } = DateTime.Now; // default

    public decimal Subtotal { get; set; }

    public decimal Impuestos { get; set; }

    public decimal Total { get; set; }

    public int? IdMantenimiento { get; set; }

    [MaxLength(20)]
    public string? NumeroFactura { get; set; }
    public virtual ICollection<DetalleComprobante> DetalleComprobantes { get; set; } = new List<DetalleComprobante>();

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual Mantenimiento? IdMantenimientoNavigation { get; set; }
}
