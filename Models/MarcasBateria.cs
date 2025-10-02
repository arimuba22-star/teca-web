using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class MarcasBateria
{
    public int IdMarca { get; set; }

    public int IdTipoSistema { get; set; }

    public string? NombreMarca { get; set; }

    public bool? NecesitaMantenimiento { get; set; }

    public virtual TiposSistema IdTipoSistemaNavigation { get; set; } = null!;
}
