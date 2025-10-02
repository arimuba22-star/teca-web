using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SWebEnergia.Controllers
{
    public class ProductosController : Controller
    {
        private readonly EnergiaContext _context;
        private readonly string _rutaImagenes;

        public ProductosController(EnergiaContext context, IWebHostEnvironment env)
        {
            _context = context;
            _rutaImagenes = Path.Combine(env.WebRootPath, "images/productos");
            if (!Directory.Exists(_rutaImagenes))
                Directory.CreateDirectory(_rutaImagenes);
        }

        // GET: Productos/ObtenerPrecio?idProducto=5
        [HttpGet]
        public async Task<IActionResult> ObtenerPrecio(int idProducto)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == idProducto);

            if (producto == null)
                return NotFound();

            return Json(producto.PrecioVenta);
        }


        // GET: Productos
        public async Task<IActionResult> Index(string? q, string? sort)
        {
            ViewData["CurrentFilter"] = q;
            ViewData["NombreSort"] = sort == "nombre_asc" ? "nombre_desc" : "nombre_asc";
            ViewData["PrecioSort"] = sort == "precio_asc" ? "precio_desc" : "precio_asc";

            var query = _context.Productos.Include(p => p.IdCategoriaNavigation).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Nombre.Contains(q) || p.CodigoSku.Contains(q));

            query = sort switch
            {
                "nombre_desc" => query.OrderByDescending(p => p.Nombre),
                "precio_asc" => query.OrderBy(p => p.PrecioVenta),
                "precio_desc" => query.OrderByDescending(p => p.PrecioVenta),
                _ => query.OrderBy(p => p.Nombre)
            };

            return View(await query.AsNoTracking().ToListAsync());
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.IdCategoriaNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null) return NotFound();
            return View(producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            ViewBag.IdCategoria = new SelectList(_context.CategoriasProductos, "IdCategoria", "Nombre");
            return View();
        }

        // POST: Productos/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCategoria,CodigoSku,Nombre,Descripcion,PrecioCompra,PrecioVenta,Stock,StockMinimo,FrecuenciaMantenimientoRecomendado,TiempoVidaUtil,UnidadMedida,Activo")] Producto producto, IFormFile? imagen)
        {
            // Depuración: Verificar si el modelo es válido
            Console.WriteLine("Validando el modelo...");
            if (!ModelState.IsValid)
            {
                // Imprimir errores de validación para ver qué está fallando
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }

                ViewBag.IdCategoria = new SelectList(_context.CategoriasProductos, "IdCategoria", "Nombre", producto.IdCategoria);
                return View(producto);
            }

            // Depuración: Verificación de los valores del producto antes de guardar
            Console.WriteLine($"Creando producto: {producto.Nombre}, SKU: {producto.CodigoSku}");
            Console.WriteLine($"Precio de Compra: {producto.PrecioCompra}, Precio de Venta: {producto.PrecioVenta}");
            Console.WriteLine($"Stock: {producto.Stock}, Stock Mínimo: {producto.StockMinimo}");
            Console.WriteLine($"Frecuencia de Mantenimiento: {producto.FrecuenciaMantenimientoRecomendado}, Tiempo de Vida Útil: {producto.TiempoVidaUtil}");

            // Manejo de imagen
            if (imagen != null && imagen.Length > 0)
            {
                try
                {
                    string nombreArchivo = Guid.NewGuid() + Path.GetExtension(imagen.FileName);
                    string rutaCompleta = Path.Combine(_rutaImagenes, nombreArchivo);

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }

                    producto.ImagenRuta = "/images/productos/" + nombreArchivo;
                    Console.WriteLine("Imagen cargada correctamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar la imagen: {ex.Message}");
                    TempData["Error"] = "Error al cargar la imagen.";
                    return View(producto);
                }
            }
            else
            {
                Console.WriteLine("No se cargó ninguna imagen.");
            }

            producto.FechaAlta = DateTime.Now;

            try
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Producto creado correctamente.";
                Console.WriteLine("Producto guardado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar el producto: {ex.Message}");
                TempData["Error"] = $"Error al guardar el producto: {ex.Message}";
                return View(producto);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.IdCategoria = new SelectList(_context.CategoriasProductos, "IdCategoria", "Nombre", producto.IdCategoria);
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProducto,IdCategoria,CodigoSku,Nombre,Descripcion,PrecioCompra,PrecioVenta,Stock,StockMinimo,FrecuenciaMantenimientoRecomendado,TiempoVidaUtil,UnidadMedida,Activo,ImagenRuta")] Producto producto, IFormFile? imagen)
        {
            if (id != producto.IdProducto) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.IdCategoria = new SelectList(_context.CategoriasProductos, "IdCategoria", "Nombre", producto.IdCategoria);
                return View(producto);
            }

            try
            {
                var productoDb = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.IdProducto == id);
                if (productoDb == null) return NotFound();

                // Manejo de imagen
                if (imagen != null && imagen.Length > 0)
                {
                    // Eliminar imagen anterior si existe
                    if (!string.IsNullOrEmpty(productoDb.ImagenRuta))
                    {
                        string rutaAnterior = Path.Combine(_rutaImagenes, Path.GetFileName(productoDb.ImagenRuta));
                        if (System.IO.File.Exists(rutaAnterior))
                            System.IO.File.Delete(rutaAnterior);
                    }

                    string nombreArchivo = Guid.NewGuid() + Path.GetExtension(imagen.FileName);
                    string rutaCompleta = Path.Combine(_rutaImagenes, nombreArchivo);

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }

                    producto.ImagenRuta = "/images/productos/" + nombreArchivo;
                }
                else
                {
                    // Conservar imagen anterior si no se sube nueva
                    producto.ImagenRuta = productoDb.ImagenRuta;
                }

                _context.Update(producto);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Producto actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(e => e.IdProducto == id)) return NotFound();
                throw;
            }
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.IdCategoriaNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null) return NotFound();
            return View(producto);
        }

        // POST: Productos/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.DetalleMantenimientos)
                .Include(p => p.DetalleVenta)
                .Include(p => p.DetalleComprobantes)
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar las dependencias manualmente si existen
            if (producto.DetalleMantenimientos.Any())
            {
                // Eliminar los mantenimientos relacionados
                _context.DetalleMantenimientos.RemoveRange(producto.DetalleMantenimientos);
            }

            if (producto.DetalleVenta.Any())
            {
                // Eliminar las ventas relacionadas
                _context.DetalleVenta.RemoveRange(producto.DetalleVenta);
            }

            if (producto.DetalleComprobantes.Any())
            {
                // Eliminar los comprobantes relacionados
                _context.DetalleComprobantes.RemoveRange(producto.DetalleComprobantes);
            }

            // Eliminar imagen si existe
            if (!string.IsNullOrEmpty(producto.ImagenRuta))
            {
                var ruta = Path.Combine(_rutaImagenes, Path.GetFileName(producto.ImagenRuta));
                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);
            }

            // Eliminar el producto
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Producto y sus relaciones eliminadas correctamente.";
            return RedirectToAction(nameof(Index));
        }




    }
}
