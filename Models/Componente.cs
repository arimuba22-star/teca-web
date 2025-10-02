using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWebEnergia.Models
{
    public class Componente
    {
        [Key]
        public int? IdComponente { get; set; }

        public int IdSistema { get; set; } // FK hacia SistemasRenovables
        public int IdTipoComponente { get; set; }  // FK hacia TipoComponente

        public string? Descripcion { get; set; }

        public decimal? CapacidadEnergetica { get; set; }

        public DateTime? FechaInstalacion { get; set; }

        public DateTime? UltimoFechaMantenimiento { get; set; }

        // 👇 Asocia explícitamente la FK
        [ForeignKey("IdSistema")]
        public virtual SistemasRenovable? Sistema { get; set; }

        [ForeignKey("IdTipoComponente")]
        public virtual TipoComponente? TipoComponente { get; set; }
    }
}
