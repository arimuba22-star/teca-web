using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Properties;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace SWebEnergia.Controllers
{
    public class VentasController : Controller
    {
        private readonly EnergiaContext _context;
        private readonly ILogger<VentasController> _logger;  // campo para logger

        public VentasController(EnergiaContext context, ILogger<VentasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Ventas
        public async Task<IActionResult> Index(string q, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            ViewData["CurrentFilter"] = q;
            ViewData["FechaDesde"] = fechaDesde?.ToString("yyyy-MM-dd");
            ViewData["FechaHasta"] = fechaHasta?.ToString("yyyy-MM-dd");

            var ventas = _context.Ventas
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.IdMantenimientoNavigation)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                ventas = ventas.Where(v => v.IdClienteNavigation.Nombre.Contains(q));
            }

            if (fechaDesde.HasValue)
            {
                ventas = ventas.Where(v => v.Fecha >= fechaDesde.Value);
            }
            if (fechaHasta.HasValue)
            {
                ventas = ventas.Where(v => v.Fecha <= fechaHasta.Value);
            }

            return View(await ventas.ToListAsync());
        }

        // GET: Ventas/Details/5
        // GET: Ventas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venta = await _context.Ventas
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.IdMantenimientoNavigation)
                .ThenInclude(m => m.SistemaRenovable) // <--- ¡Añade esta línea!
                .Include(v => v.DetalleVenta)
                .ThenInclude(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(m => m.IdVenta == id);

            if (venta == null) return NotFound();

            return View(venta);
        }

        private string GenerarNumeroComprobante(bool esFactura)
        {
            var ahora = DateTime.Now;
            string mes = ahora.ToString("MM");

            // Filtro según tipo de comprobante (factura = RUC, boleta = DNI)
            var comprobantesDelMes = _context.Ventas
                .Where(v => v.NumeroComprobante != null &&
                            v.NumeroComprobante.StartsWith(mes + "-") &&
                            v.Fecha.HasValue &&
                            v.Fecha.Value.Year == ahora.Year &&
                            v.Fecha.Value.Month == ahora.Month &&
                            (
                                esFactura
                                ? v.IdClienteNavigation.NDocumento.Length == 11
                                : v.IdClienteNavigation.NDocumento.Length < 11
                            ))
                .Select(v => v.NumeroComprobante)
                .ToList();

            int maxNumero = 0;
            foreach (var numero in comprobantesDelMes)
            {
                var partes = numero.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int n))
                {
                    if (n > maxNumero)
                        maxNumero = n;
                }
            }

            int nuevoNumero = maxNumero + 1;
            return $"{mes}-{nuevoNumero:D4}"; 
        }



        [HttpGet]
       
        public IActionResult GetMantenimientosPorCliente(int idCliente)
        {
            var mantenimientos = _context.Mantenimientos
                .Include(m => m.SistemaRenovable)
                .Where(m => m.IdCliente == idCliente)
                .Select(m => new
                {
                    m.IdMantenimiento,
                    Nombre = (m.SistemaRenovable != null ? m.SistemaRenovable.Descripcion : "Sin sistema") +
                             " - " +
                             (m.FechaProgramada.HasValue ? m.FechaProgramada.Value.ToString("dd/MM/yyyy") : "Sin fecha")
                })
                .ToList();

            return Json(mantenimientos);
        }

        // ⚠️ Método para la DESCARGA del PDF
        public async Task<IActionResult> GenerarPdf(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.DetalleVenta)
                .ThenInclude(dv => dv.IdProductoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null) return NotFound();

            string plantillaPath;
            bool esFactura = venta.IdClienteNavigation?.NDocumento?.Length == 11;

            if (esFactura)
            {
                plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfTemplates/plantilla_factura.pdf");
            }
            else
            {
                plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfTemplates/plantilla_boleta.pdf");
            }

            if (!System.IO.File.Exists(plantillaPath))
            {
                return NotFound("No se encontró la plantilla de PDF correspondiente.");
            }

            using var msOutput = new MemoryStream();
            using var pdfReader = new PdfReader(plantillaPath);
            using var pdfWriter = new PdfWriter(msOutput);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            var document = new Document(pdfDoc);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            document.SetFont(font);

           var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            string tipoDocumento2 = esFactura ? "FACTURA" : "BOLETA";
            string numero2 = venta.NumeroComprobante ?? "N/A";

            // Se aplica el tamaño de la fuente directamente al Paragraph
            document.ShowTextAligned(
                new Paragraph($"{numero2}")
                    .SetFont(boldFont)
                    .SetFontSize(13), // <-- ¡Aquí está la corrección!
                495, 776, TextAlignment.LEFT);


            // los demás datos iguales...
            document.ShowTextAligned(new Paragraph($"{venta.Fecha:dd/MM/yyyy}"), 120, 684, TextAlignment.LEFT);
            var cliente3 = venta.IdClienteNavigation;
            document.ShowTextAligned(new Paragraph($"{cliente3.Nombre}"), 134, 655, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{cliente3.NDocumento ?? "N/A"}"), 105, 628, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{cliente3.Direccion ?? "No registrada"}"), 150, 600, TextAlignment.LEFT);

            float yTable2 = 516;
            foreach (var detalle in venta.DetalleVenta)
            {
                document.ShowTextAligned(new Paragraph($"{detalle.Cantidad}"), 400, yTable2, TextAlignment.LEFT);
                document.ShowTextAligned(new Paragraph($"{detalle.IdProductoNavigation?.Nombre}"), 80, yTable2, TextAlignment.LEFT);
                document.ShowTextAligned(new Paragraph($"{detalle.PrecioUnitario}"), 315, yTable2, TextAlignment.LEFT);
                document.ShowTextAligned(new Paragraph($"{(detalle.Cantidad * detalle.PrecioUnitario)}"), 470, yTable2, TextAlignment.LEFT);
                yTable2 -= 15;
            }

            document.ShowTextAligned(new Paragraph($"{venta.Subtotal}"), 405, 318, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{venta.Impuestos}"), 405, 288, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{venta.Total}"), 405, 252, TextAlignment.LEFT);

            document.Close();
            var pdfBytes = msOutput.ToArray();
            return File(pdfBytes, "application/pdf", $"Venta_{venta.NumeroComprobante ?? venta.IdVenta.ToString()}.pdf");
        }

        // ⚠️ Método para la VISTA PREVIA del PDF (no fuerza la descarga)
        public async Task<IActionResult> VistaPreviaPdf(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.DetalleVenta)
                .ThenInclude(dv => dv.IdProductoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null) return NotFound();

            string plantillaPath;
            bool esFactura = venta.IdClienteNavigation?.NDocumento?.Length == 11;

            if (esFactura)
            {
                plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfTemplates/plantilla_factura.pdf");
            }
            else
            {
                plantillaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfTemplates/plantilla_boleta.pdf");
            }

            if (!System.IO.File.Exists(plantillaPath))
            {
                return NotFound("No se encontró la plantilla de PDF correspondiente.");
            }

            using var msOutput = new MemoryStream();
            using var pdfReader = new PdfReader(plantillaPath);
            using var pdfWriter = new PdfWriter(msOutput);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            var document = new Document(pdfDoc);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            document.SetFont(font);

            // Mostrar tipo y número
            // Mostrar tipo y número
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            string tipoDocumento2 = esFactura ? "FACTURA" : "BOLETA";
            string numero2 = venta.NumeroComprobante ?? "N/A";

            // Se aplica el tamaño de la fuente directamente al Paragraph
            document.ShowTextAligned(
                new Paragraph($"{numero2}")
                    .SetFont(boldFont)
                    .SetFontSize(13), // <-- ¡Aquí está la corrección!
                500, 790, TextAlignment.LEFT);


            // los demás datos iguales...
            document.ShowTextAligned(new Paragraph($"{venta.Fecha:dd/MM/yyyy}"), 120, 692, TextAlignment.LEFT);
            var cliente3 = venta.IdClienteNavigation;
            document.ShowTextAligned(new Paragraph($"{cliente3.Nombre}"), 134, 668, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{cliente3.NDocumento ?? "N/A"}"), 123, 645, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{cliente3.Direccion ?? "No registrada"}"), 150, 623, TextAlignment.LEFT);

            float yTable2 = 516;
            foreach (var detalle in venta.DetalleVenta)
            {
                document.ShowTextAligned(new Paragraph($"{detalle.Cantidad}"), 400, yTable2, TextAlignment.LEFT);
                document.ShowTextAligned(new Paragraph($"{detalle.IdProductoNavigation?.Nombre}"), 80, yTable2, TextAlignment.LEFT);
                document.ShowTextAligned(new Paragraph($"{detalle.PrecioUnitario}"), 315, yTable2, TextAlignment.LEFT);
                document.ShowTextAligned(new Paragraph($"{(detalle.Cantidad * detalle.PrecioUnitario)}"), 470, yTable2, TextAlignment.LEFT);
                yTable2 -= 15;
            }

            document.ShowTextAligned(new Paragraph($"{venta.Subtotal}"), 405, 350, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{venta.Impuestos}"), 405, 321, TextAlignment.LEFT);
            document.ShowTextAligned(new Paragraph($"{venta.Total}"), 405, 283, TextAlignment.LEFT);

            document.Close();
            var pdfBytes = msOutput.ToArray();
            return File(pdfBytes, "application/pdf");
        }

        public IActionResult Create()
        {
            var clientes = _context.Clientes.ToList();
            ViewData["Clientes"] = clientes;
            ViewData["Productos"] = _context.Productos.ToList();

            var mantenimientos = _context.Mantenimientos
               .Include(m => m.SistemaRenovable)
               .Select(m => new
               {
                   m.IdMantenimiento,
                   Nombre = m.SistemaRenovable.Descripcion + " - " +
                             (m.FechaProgramada.HasValue ? m.FechaProgramada.Value.ToString("dd/MM/yyyy") : "Sin fecha")
               })
               .ToList();
            ViewData["Mantenimientos"] = new SelectList(mantenimientos, "IdMantenimiento", "Nombre");

            return View(new Venta { Fecha = DateTime.Now });
        }

        // POST: Ventas/CrearVentaConDetalles (Nuevo método para manejar el JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Venta venta)
        {
            _logger.LogInformation("Método Create llamado en {Fecha}", System.DateTime.Now);

            if (venta == null)
            {
                _logger.LogWarning("El objeto venta es null");
                return BadRequest("El objeto venta es null");
            }

            if (venta.DetalleVenta == null || !venta.DetalleVenta.Any())
            {
                _logger.LogWarning("La venta no contiene productos");
                return BadRequest("La venta no contiene productos.");
            }

            try
            {
                _logger.LogInformation("Venta recibida: {@Venta}", venta);

                // Calcular importes de detalles
                foreach (var detalle in venta.DetalleVenta)
                {
                    detalle.Importe = detalle.Cantidad * detalle.PrecioUnitario;
                    _logger.LogInformation("Detalle: Producto {IdProducto}, Cantidad {Cantidad}, PrecioUnitario {PrecioUnitario}, Importe {Importe}",
                        detalle.IdProducto, detalle.Cantidad, detalle.PrecioUnitario, detalle.Importe);
                }

                venta.Subtotal = venta.DetalleVenta.Sum(d => d.Importe);
                venta.Impuestos = venta.Subtotal * 0.18m;
                venta.Total = venta.Subtotal + venta.Impuestos;
                venta.Fecha = System.DateTime.Now;

                // ** NUEVO: cargar al cliente para saber si es factura o boleta **
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.IdCliente == venta.IdCliente);
                if (cliente == null)
                {
                    _logger.LogWarning("Cliente no encontrado al generar numero comprobante");
                    return BadRequest("Cliente no válido.");
                }

                bool esFactura = cliente.NDocumento != null && cliente.NDocumento.Length == 11;
                venta.NumeroComprobante = GenerarNumeroComprobante(esFactura);

                // guardar la venta con el número ya asignado
                _context.Add(venta);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Venta guardada correctamente con IdVenta {IdVenta}, NumeroComprobante {Numero}", venta.IdVenta, venta.NumeroComprobante);

                TempData["Ok"] = "Venta registrada correctamente.";
                return Ok(new { idVenta = venta.IdVenta, numeroComprobante = venta.NumeroComprobante });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la venta");
                return StatusCode(500, "Error interno al registrar la venta");
            }
        }


        // GET: Ventas/Edit/5
        // GET: Ventas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venta = await _context.Ventas
                .Include(v => v.DetalleVenta)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null) return NotFound();

            // Asegurarse de que ViewData se carga correctamente
            await CargarViewDataParaVenta(venta.IdCliente, venta.IdMantenimiento);

            // ✅ CORRECCIÓN: Generar el JSON de productos simplificados después de cargar ViewData
            var productosSimplificados = _context.Productos
                .Select(p => new {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre,
                    PrecioVenta = p.PrecioVenta
                })
                .ToList();

            ViewData["ProductosJson"] = JsonConvert.SerializeObject(productosSimplificados);

            return View(venta);
        }

        // POST: Ventas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] Venta ventaConDetalles)
        {
            if (id != ventaConDetalles.IdVenta)
            {
                return NotFound();
            }

            var ventaExistente = await _context.Ventas
                .Include(v => v.DetalleVenta)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (ventaExistente == null)
            {
                return NotFound();
            }

            // Actualizar las propiedades de la venta principal
            ventaExistente.IdCliente = ventaConDetalles.IdCliente;
            ventaExistente.IdMantenimiento = ventaConDetalles.IdMantenimiento;
            ventaExistente.Fecha = ventaConDetalles.Fecha;

            // Lógica para manejar los productos (DetalleVenta)
            var detallesEnviados = ventaConDetalles.DetalleVenta ?? new List<DetalleVentum>();

            // Eliminar los detalles que ya no están en la lista
            var detallesAEliminar = ventaExistente.DetalleVenta
                .Where(d => !detallesEnviados.Any(v => v.IdProducto == d.IdProducto))
                .ToList();
            _context.DetalleVenta.RemoveRange(detallesAEliminar);

            // Actualizar o agregar los detalles
            foreach (var detalleVentaNuevo in detallesEnviados)
            {
                var detalleExistente = ventaExistente.DetalleVenta
                    .FirstOrDefault(d => d.IdProducto == detalleVentaNuevo.IdProducto);

                if (detalleExistente != null)
                {
                    // El producto ya existe, actualizarlo
                    detalleExistente.Cantidad = detalleVentaNuevo.Cantidad;
                    detalleExistente.PrecioUnitario = detalleVentaNuevo.PrecioUnitario;
                }
                else
                {
                    // Es un nuevo producto, agregarlo
                    ventaExistente.DetalleVenta.Add(detalleVentaNuevo);
                }
            }

            // Recalcular Subtotal, Impuestos y Total
            if (ventaExistente.DetalleVenta != null && ventaExistente.DetalleVenta.Any())
            {
                ventaExistente.Subtotal = ventaExistente.DetalleVenta.Sum(d => d.Cantidad * d.PrecioUnitario);
                ventaExistente.Impuestos = ventaExistente.Subtotal * 0.18m;
                ventaExistente.Total = ventaExistente.Subtotal + ventaExistente.Impuestos;
            }
            else
            {
                // Si no hay detalles, reinicia todos los valores a cero.
                ventaExistente.Subtotal = 0;
                ventaExistente.Impuestos = 0;
                ventaExistente.Total = 0;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Venta actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Ventas.AnyAsync(e => e.IdVenta == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // Método auxiliar para cargar los ViewData de forma centralizada
        private async Task CargarViewDataParaVenta(int? idCliente = null, int? idMantenimiento = null)
        {
            ViewData["Clientes"] = new SelectList(await _context.Clientes.ToListAsync(), "IdCliente", "Nombre", idCliente);
            ViewData["Productos"] = await _context.Productos.ToListAsync();

            if (idCliente.HasValue)
            {
                var mantenimientos = await _context.Mantenimientos
                    .Include(m => m.SistemaRenovable)
                    .Where(m => m.IdCliente == idCliente.Value)
                    .Select(m => new
                    {
                        m.IdMantenimiento,
                        Nombre = m.SistemaRenovable.Descripcion + " - " +
                                 (m.FechaProgramada.HasValue ? m.FechaProgramada.Value.ToString("dd/MM/yyyy") : "Sin fecha")
                    })
                    .ToListAsync();
                ViewData["Mantenimientos"] = new SelectList(mantenimientos, "IdMantenimiento", "Nombre", idMantenimiento);
            }
            else
            {
                ViewData["Mantenimientos"] = new SelectList(Enumerable.Empty<SelectListItem>(), "IdMantenimiento", "Nombre");
            }
        }

        // GET: Ventas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venta = await _context.Ventas
                .Include(v => v.IdClienteNavigation)
                .FirstOrDefaultAsync(m => m.IdVenta == id);

            if (venta == null) return NotFound();

            return View(venta);
        }

        // POST: Ventas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalles = _context.DetalleVenta.Where(d => d.IdVenta == id);
            _context.DetalleVenta.RemoveRange(detalles);

            var venta = await _context.Ventas.FindAsync(id);
            if (venta != null)
            {
                _context.Ventas.Remove(venta);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Venta eliminada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

      
    }
}