using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class VwMantenimientosPorCliente
{
    public int IdCliente { get; set; }

    public string Cliente { get; set; } = null!;

    public int? TotalMantenimientos { get; set; }

    public decimal? TotalCosto { get; set; }
}
