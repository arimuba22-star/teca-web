using System;
using System.Collections.Generic;

namespace SWebEnergia.Models.ViewModels
{
    public class MantenimientoViewModel
    {
        // PK (para editar)
        public int IdMantenimiento { get; set; }

        // Cliente / Sistemas
        public int IdCliente { get; set; }
        public List<Cliente> Clientes { get; set; } = new List<Cliente>();

        public int IdSistema { get; set; } // <-- cambiar IdSistemaRenovable a IdSistema
        public List<SistemasRenovable> Sistemas { get; set; } = new List<SistemasRenovable>();

        // Info general
        public string TipoMantenimiento { get; set; } = "Preventivo"; // Correctivo/Preventivo
        public DateTime? FechaSolicitud { get; set; } = DateTime.Now;
        public DateTime? FechaProgramada { get; set; }
        public DateTime? FechaFin { get; set; }

        // Estado / costos
        public string Estado { get; set; } = "Pendiente";
        public decimal? CostoMantenimiento { get; set; }
        public decimal? CostoTotal { get; set; }

        // Productos asociados
        public bool RequiereProductos { get; set; }
        public List<DetalleMantenimientoViewModel> Detalles { get; set; } = new List<DetalleMantenimientoViewModel>();

        // Lista de todos los productos para mostrar en tabla
        public List<Producto> Productos { get; set; } = new List<Producto>();

        // Técnicos
        public string? Diagnostico { get; set; }
        public string? Observaciones { get; set; }

        // Tipo de Comprobante (Factura o Boleta)
        public string? TipoComprobante { get; set; }


    }
}