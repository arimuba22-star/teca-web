using System;
using System.Collections.Generic;

namespace SWebEnergia.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClientes { get; set; }
        public int TotalProductos { get; set; }
        public int TotalVentas { get; set; }
        public int TotalMantenimientos { get; set; }

        // Datos para gráficas (últimos 6 meses)
        public List<string> SalesChartLabels { get; set; } = new();
        public List<decimal> SalesChartTotals { get; set; } = new();
        public List<int> MaintenanceChartTotals { get; set; } = new();
    }
}
