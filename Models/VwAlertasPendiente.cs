using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class VwAlertasPendiente
{
    public int IdAlerta { get; set; }

    public int IdSistema { get; set; }

    public string TipoSistema { get; set; } = null!;

    public string? Tipo { get; set; }

    public string? Mensaje { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaGeneracion { get; set; }
}
