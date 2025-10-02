using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class TiposSistema
{
    public int IdTipoSistema { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<MarcasBateria> MarcasBateria { get; set; } = new List<MarcasBateria>();

    public virtual ICollection<SistemasRenovable> SistemasRenovables { get; set; } = new List<SistemasRenovable>();
}
