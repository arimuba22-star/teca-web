using System.Collections.Generic;

namespace SWebEnergia.Models.ViewModels
{
    public class ReporteGeneralViewModel
    {
        public List<Reporte> ReportesVentas { get; set; } = new();
        public List<ReporteMantenimiento> ReportesMantenimientos { get; set; } = new();
    }
}
