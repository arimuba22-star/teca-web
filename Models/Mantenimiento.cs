using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWebEnergia.Models
{
    public class Mantenimiento
    {
        [Key]
        public int IdMantenimiento { get; set; }

        [Required]
        [ForeignKey("SistemaRenovable")]
        [Column("IdSistema")]
        public int IdSistema { get; set; }

        [Required]
        [ForeignKey("Cliente")]
        [Column("IdCliente")]
        public int IdCliente { get; set; }

        public int? IdGrupoTecnico { get; set; }

        [Required]
        [MaxLength(20)]
        public string TipoMantenimiento { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public DateTime? FechaProgramada { get; set; }

        [NotMapped]
        public DateTime? FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; }
        public string? Diagnostico { get; set; }
        public string? TrabajosRealizados { get; set; }
        public string? Observaciones { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? CostoTotal { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? CostoMantenimiento { get; set; }
        public virtual Cliente Cliente { get; set; }
        public virtual SistemasRenovable SistemaRenovable { get; set; }

        public virtual ICollection<DetalleMantenimiento> Detalles { get; set; } = new List<DetalleMantenimiento>();

        // Relación con Comprobante
        public virtual ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();

        // Propiedad auxiliar (no está en la DB)
        [NotMapped]
        public bool RequiereProductos { get; set; }

    }
}
