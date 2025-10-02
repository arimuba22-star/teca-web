using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWebEnergia.Models;

[Table("Alertas")]
public partial class Alerta
{
    [Key]
    public int IdAlerta { get; set; }

    [ForeignKey("Sistema")]
    public int IdSistema { get; set; }

    public string? Tipo { get; set; }

    public string? Mensaje { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaGeneracion { get; set; }

    public DateTime? FechaResolucion { get; set; }

    // Relación con SistemaRenovable
    public virtual SistemasRenovable IdSistemaNavigation { get; set; } = null!;
}