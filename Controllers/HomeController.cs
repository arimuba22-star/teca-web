using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;           // DbContext y entidades scaffold
using SWebEnergia.ViewModels;       // nuestro ViewModel
using System;
using System.Linq;

namespace SWebEnergia.Controllers
{
    public class HomeController : Controller
    {
        private readonly EnergiaContext _context;
        public HomeController(EnergiaContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var vm = new DashboardViewModel();

            // Totales simples
            vm.TotalClientes = _context.Clientes.Count();
            vm.TotalProductos = _context.Productos.Count();
            vm.TotalVentas = _context.Ventas.Count();
            vm.TotalMantenimientos = _context.Mantenimientos.Count();

            // Rango: últimos 6 meses (incluye mes actual)
            var hoy = DateTime.Now;
            var primerMes = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-5);

            // Ventas por mes (suma total)
            var ventasPorMes = _context.Ventas
                .Where(v => v.Fecha != null && v.Fecha >= primerMes)
                .GroupBy(v => new { Year = v.Fecha.Value.Year, Month = v.Fecha.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => (decimal?)x.Total) ?? 0m })
                .ToList();

            // Mantenimientos por mes (conteo)
            //var mantPorMes = _context.Mantenimientos
            //    .Where(m => m.FechaSolicitud != null && m.FechaSolicitud >= primerMes)
            //    .GroupBy(m => new { Year = m.FechaSolicitud.Value.Year, Month = m.FechaSolicitud.Value.Month })
            //    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            //    .ToList();

            // Llenar etiquetas y datos para los 6 meses en orden
            //for (int i = 0; i < 6; i++)
            //{
            //    var dt = primerMes.AddMonths(i);
            //    vm.SalesChartLabels.Add(dt.ToString("MMM yyyy"));

            //    var venta = ventasPorMes.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month);
            //    vm.SalesChartTotals.Add(venta?.Total ?? 0m);

            //    var mant = mantPorMes.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month);
            //    vm.MaintenanceChartTotals.Add(mant?.Count ?? 0);
            //}

            return View(vm);
        }
    }
}
