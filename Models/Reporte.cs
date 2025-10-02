using System;
using System.Collections.Generic;



namespace SWebEnergia.Models
{
    public class Reporte
    {
        public int IdVenta { get; set; }            // NUEVO
        public string NombreCliente { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Impuestos { get; set; }
        public decimal? Total { get; set; }
        public DateTime FechaVenta { get; set; }
    }

}
