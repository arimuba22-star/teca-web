using SWebEnergia.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class TipoComponente
{
    public int IdTipoComponente { get; set; }
    public string? Descripcion { get; set; }
    public int TiempoVida { get; set; }
    public decimal? FrecuenciaMantenimientoYear { get; set; }

    [NotMapped]  // No se guarda en la BD
    public decimal? FrecuenciaMantenimientoMes
    {
        get
        {
            return FrecuenciaMantenimientoYear.HasValue
                ? FrecuenciaMantenimientoYear.Value * 12
                : null;
        }
    }

    public virtual ICollection<Componente> Componentes { get; set; } = new List<Componente>();
}
