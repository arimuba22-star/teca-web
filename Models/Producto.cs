using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SWebEnergia.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "El código SKU es obligatorio")]
    [StringLength(50)]
    [Display(Name = "Código SKU")]
    public string CodigoSku { get; set; } = null!;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(0, 9999999, ErrorMessage = "El precio de compra no es válido")]
    [Display(Name = "Precio Compra")]
    public decimal? PrecioCompra { get; set; }

    [Required(ErrorMessage = "El precio de venta es obligatorio")]
    [Range(0, 9999999, ErrorMessage = "El precio de venta no es válido")]
    [Display(Name = "Precio Venta")]
    public decimal PrecioVenta { get; set; }

    [Display(Name = "Stock")]
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no es válido")]
    public int Stock { get; set; } = 0; // default 0

    [Display(Name = "Stock Mínimo")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no es válido")]
    public int? StockMinimo { get; set; } = 5; // default 5

    // Nuevas propiedades
    [Display(Name = "Frecuencia Mantenimiento Recomendado (Años)")]
    [Range(1, 999999999, ErrorMessage = "La frecuencia no puede ser mayor a 9 dígitos.")]
    public int? FrecuenciaMantenimientoRecomendado { get; set; }

    [Display(Name = "Tiempo de Vida Útil (Años)")]
    [Range(1, 999999999, ErrorMessage = "El tiempo de vida útil no puede ser mayor a 9 dígitos.")]
    public int? TiempoVidaUtil { get; set; }

    [Display(Name = "Unidad de Medida")]
    [StringLength(20)]
    public string? UnidadMedida { get; set; }

    [Display(Name = "Fecha de Alta")]
    [DataType(DataType.Date)]
    public DateTime? FechaAlta { get; set; } = DateTime.Now; // default getdate()

    [Display(Name = "Activo")]
    public bool? Activo { get; set; } = true; // default 1

    [NotMapped]
    [Display(Name = "Imagen")]
    [StringLength(250)]
    public string? ImagenRuta { get; set; }

    public virtual ICollection<DetalleComprobante> DetalleComprobantes { get; set; } = new List<DetalleComprobante>();

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual CategoriasProducto? IdCategoriaNavigation { get; set; }

    [ValidateNever]
    public ICollection<DetalleMantenimiento> DetalleMantenimientos { get; set; } = new List<DetalleMantenimiento>();

}
