using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class TecnicosGrupo
{
    public int IdTecnico { get; set; }

    public int IdGrupo { get; set; }

    public DateOnly? FechaAsignacion { get; set; }

    public virtual GruposTecnico IdGrupoNavigation { get; set; } = null!;

    public virtual Tecnico IdTecnicoNavigation { get; set; } = null!;
}
