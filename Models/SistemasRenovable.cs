using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SWebEnergia.Models;

public partial class SistemasRenovable
{
    public int IdSistema { get; set; }              // PK
    public int IdCliente { get; set; }              // FK
    public int IdTipoSistema { get; set; }          // FK
    public string? Descripcion { get; set; }        // nvarchar(500)
    public DateOnly FechaInstalacion { get; set; }   // date
    public decimal? CapacidadKw { get; set; }       // decimal(10,2)
    public string? Estado { get; set; }             // nvarchar(50)
    public string? Departamento { get; set; }       // nvarchar(100)
    public string? Provincia { get; set; }          // nvarchar(100)
    public string? DireccionExacta { get; set; }    // nvarchar(255)
    public string? Referencia { get; set; }         // nvarchar(255)
    public string? Observaciones { get; set; }      // nvarchar(max)

    // 🔗 Relaciones de navegación (FK)
    public virtual Cliente? IdClienteNavigation { get; set; }
    public virtual TiposSistema? IdTipoSistemaNavigation { get; set; }

    // 🔗 Relaciones 1:N
    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();

    [BindNever]
    public virtual ICollection<Componente> Componentes { get; set; } = new List<Componente>();

}