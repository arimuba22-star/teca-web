using System;

namespace SWebEnergia.Models
{
    public class ReporteMantenimiento
    {
        public int IdMantenimiento { get; set; }
        public string NombreCliente { get; set; }
        public string TipoMantenimiento { get; set; }
        public DateTime Fecha { get; set; }
        public decimal? CostoTotal { get; set; }
    }
}
