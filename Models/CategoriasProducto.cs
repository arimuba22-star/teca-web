
using System;
using System.Collections.Generic;

namespace SWebEnergia.Models;

public partial class CategoriasProducto
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public int? CategoriaPadre { get; set; }

    public virtual CategoriasProducto? CategoriaPadreNavigation { get; set; }

    public virtual ICollection<CategoriasProducto> InverseCategoriaPadreNavigation { get; set; } = new List<CategoriasProducto>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}