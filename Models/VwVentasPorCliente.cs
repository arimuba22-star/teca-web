using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class VwVentasPorCliente
{
    public int IdCliente { get; set; }

    public string Cliente { get; set; } = null!;

    public int? TotalVentas { get; set; }

    public decimal? TotalFacturado { get; set; }
}
