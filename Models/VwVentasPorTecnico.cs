using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class VwVentasPorTecnico
{
    public int IdTecnico { get; set; }

    public string NombreTecnico { get; set; } = null!;

    public int? TotalVentas { get; set; }
}
