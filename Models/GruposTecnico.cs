using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class GruposTecnico
{
    public int IdGrupo { get; set; }

    public string? Nombre { get; set; }

    public int? Responsable { get; set; }

    public virtual Tecnico? ResponsableNavigation { get; set; }

    public virtual ICollection<TecnicosGrupo> TecnicosGrupos { get; set; } = new List<TecnicosGrupo>();
}
