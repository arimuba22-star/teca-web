using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace SWebEnergia.Models.ViewModels
{
    public class DetalleMantenimientoViewModel
    {
        public int IdDetalle { get; set; }

        // Producto seleccionado
        public int IdProducto { get; set; }
        public string? NombreProducto { get; set; }

        // Datos de cantidad / precio   
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        // Nuevo: indica si el producto está seleccionado en la vista
        public bool Seleccionado { get; set; }
        // Calculado en el VM para mostrar en la vista
        public decimal SubTotal { get; set; }
    }
}
