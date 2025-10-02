using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class DetalleComprobante
{
    public int IdDetalle { get; set; }

    public int IdComprobante { get; set; }

    public int? IdProducto { get; set; }

    public string Concepto { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Computed)]
    public decimal? Importe { get; set; }

    public virtual Comprobante IdComprobanteNavigation { get; set; } = null!;

    public virtual Producto? IdProductoNavigation { get; set; }
}
