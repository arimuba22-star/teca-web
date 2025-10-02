using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class VwHistorialMantenimiento
{
    public int IdCliente { get; set; }

    public string Cliente { get; set; } = null!;

    public int IdSistema { get; set; }

    public string? Sistema { get; set; }

    public int IdMantenimiento { get; set; }

    public string TipoMantenimiento { get; set; } = null!;

    public DateTime? FechaSolicitud { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string Estado { get; set; } = null!;

    public int? IdProducto { get; set; }

    public string? Producto { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal? PrecioUnitario { get; set; }

    public decimal? TotalProducto { get; set; }
}
