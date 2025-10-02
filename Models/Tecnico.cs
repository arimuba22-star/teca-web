using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class Tecnico
{
    public int IdTecnico { get; set; }

    public int? IdUsuario { get; set; }

    public string? Especialidad { get; set; }

    public DateOnly? FechaContratacion { get; set; }

    public virtual ICollection<GruposTecnico> GruposTecnicos { get; set; } = new List<GruposTecnico>();

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<TecnicosGrupo> TecnicosGrupos { get; set; } = new List<TecnicosGrupo>();
}
