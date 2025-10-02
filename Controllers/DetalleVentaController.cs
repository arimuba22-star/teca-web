using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using System.Linq;
using System.Threading.Tasks;

public class DetalleVentaController : Controller
{
    private readonly EnergiaContext _context;

    public DetalleVentaController(EnergiaContext context)
    {
        _context = context;
    }

    // GET: DetalleVenta/Create?idVenta=5
    public IActionResult Create(int idVenta)
    {
        ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "Nombre");

        var detalle = new DetalleVentum
        {
            IdVenta = idVenta
        };

        return View(detalle);
    }


    // POST: DetalleVenta/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdVenta,IdProducto,Cantidad,PrecioUnitario")] DetalleVentum detalle)
    {
        if (!ModelState.IsValid)
        {
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "Nombre", detalle.IdProducto);
            ViewData["IdVenta"] = detalle.IdVenta;
            return View(detalle);
        }

        // Calcular importe
        detalle.Importe = detalle.Cantidad * detalle.PrecioUnitario;

        _context.Add(detalle);
        await _context.SaveChangesAsync();

        // Actualizar totales en venta
        var venta = await _context.Ventas.FindAsync(detalle.IdVenta);
        if (venta != null)
        {
            venta.Subtotal = _context.DetalleVenta
                .Where(d => d.IdVenta == detalle.IdVenta)
                .Sum(d => (decimal?)d.Importe) ?? 0m;


            venta.Impuestos = venta.Subtotal * 0.18m;  // ejemplo 18% impuesto
            venta.Total = venta.Subtotal + venta.Impuestos;

            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Ventas", new { id = detalle.IdVenta });
    }
}
