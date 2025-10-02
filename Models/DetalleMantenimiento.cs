using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SWebEnergia.Models;

public class DetalleMantenimiento
{
    [Key]
    [Column("IdDetalle")]
    public int IdDetalle { get; set; }

    public int IdMantenimiento { get; set; }
    public virtual Mantenimiento Mantenimiento { get; set; } = null!;

    public int IdProducto { get; set; }
    public virtual Producto Producto { get; set; } = null!;

    // 📦 Datos de la operación
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    // 💰 Se calcula = Cantidad * PrecioUnitario

    [Column(TypeName = "decimal(12,2)")]
    public decimal SubTotal { get; set; }

   
}
