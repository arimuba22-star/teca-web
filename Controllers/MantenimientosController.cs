using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SWebEnergia.Models;
using SWebEnergia.Models.ViewModels;
using System;
using IOPath = System.IO.Path;


namespace SWebEnergia.Controllers
{
    public class MantenimientosController : Controller
    {
        private readonly EnergiaContext _context;

        public MantenimientosController(EnergiaContext context)
        {
            _context = context;
        }

        // INDEX con filtros básicos (cliente, tipo, rango fechas, estado)
        public async Task<IActionResult> Index(string? cliente, string? tipo, DateTime? desde, DateTime? hasta, string? estado)
        {
            var query = _context.Mantenimientos
                .Include(m => m.Cliente)
                .Include(m => m.SistemaRenovable)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(cliente))
                query = query.Where(m => m.Cliente.Nombre.Contains(cliente));

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(m => m.TipoMantenimiento == tipo);

            if (desde.HasValue)
                query = query.Where(m => m.FechaProgramada >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(m => m.FechaProgramada <= hasta.Value);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(m => m.Estado == estado);

            var lista = await query
                .OrderByDescending(m => m.FechaProgramada)
                .ToListAsync();

            // Guardar filtros para la vista (inputs)
            ViewData["cliente"] = cliente;
            ViewData["tipo"] = tipo;
            ViewData["desde"] = desde?.ToString("yyyy-MM-dd");
            ViewData["hasta"] = hasta?.ToString("yyyy-MM-dd");
            ViewData["estado"] = estado;

            return View(lista); // Vista Index espera IEnumerable<Mantenimiento>
        }
        [HttpGet]
        public async Task<IActionResult> ClientePorId(int idCliente)
        {
            var cliente = await _context.Clientes
                .Where(c => c.IdCliente == idCliente)
                .Select(c => new { c.IdCliente, c.Nombre })
                .FirstOrDefaultAsync();

            return Json(cliente);
        }

        public async Task<IActionResult> ComprobantePdf(int id)
        {
            var comprobante = await _context.Comprobantes
                .Include(c => c.IdClienteNavigation)
                .Include(c => c.DetalleComprobantes)
                .FirstOrDefaultAsync(c => c.IdComprobante == id);

            if (comprobante == null)
                return NotFound();

            return new ViewAsPdf("DetallesComprobante", comprobante)
            {
                FileName = $"Comprobante_{comprobante.IdComprobante}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                ViewData = { ["DisableLayout"] = true }  // Desactiva el layout para el PDF
            };
        }

        public async Task<IActionResult> ComprobantePdfInline(int id)
        {
            var comprobante = await _context.Comprobantes
                .Include(c => c.IdClienteNavigation)
                .Include(c => c.DetalleComprobantes)
                .FirstOrDefaultAsync(c => c.IdComprobante == id);

            if (comprobante == null)
                return NotFound();

            var pdfResult = new ViewAsPdf("DetallesComprobante", comprobante)
            {
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                ViewData = { ["DisableLayout"] = true }
            };

            byte[] pdfBytes = await pdfResult.BuildFile(ControllerContext);

            // Devuelve el PDF para ser mostrado inline en el iframe
            return File(pdfBytes, "application/pdf", fileDownloadName: null);
        }

        public IActionResult ComprobantePdfConPlantilla(int id)
        {
            var comprobante = _context.Comprobantes
                .Include(c => c.IdClienteNavigation)
                .Include(c => c.DetalleComprobantes)
                .Include(c => c.IdMantenimientoNavigation)
                    .ThenInclude(m => m.SistemaRenovable)
                .FirstOrDefault(c => c.IdComprobante == id);

            if (comprobante == null)
                return NotFound();

            string plantillaPath = comprobante.Tipo == "Boleta"
                ? IOPath.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfTemplates/boleta_mantenimiento.pdf")
                : IOPath.Combine(Directory.GetCurrentDirectory(), "wwwroot/pdfTemplates/factura_mantenimiento.pdf");

            using var msOutput = new MemoryStream();
            using var pdfReader = new PdfReader(plantillaPath);
            using var pdfWriter = new PdfWriter(msOutput);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var document = new Document(pdfDoc);
            document.SetFont(font);

            var cliente = comprobante.IdClienteNavigation;

            // Seccion para las coordenadas
            float numeroComprobanteX, numeroComprobanteY;
            float clienteNombreX, clienteNombreY;
            float clienteNDocumentoX, clienteNDocumentoY;
            float clienteDireccionX, clienteDireccionY;
            float fechaX, fechaY;
            float sistemaX, sistemaY;
            float tipoMantenimientoX, tipoMantenimientoY;
            float fechaProgramadaX, fechaProgramadaY;
            float trabajosRealizadosX, trabajosRealizadosY;
            float yPositionDetalles; // Posicion Y para la tabla de detalles
            float subtotalX, subtotalY;
            float impuestosX, impuestosY;
            float totalX, totalY;

            // Asignar coordenadas según el tipo de documento
            if (comprobante.Tipo == "Boleta")
            {
                numeroComprobanteX = 510; numeroComprobanteY = 784;
                clienteNombreX = 130; clienteNombreY = 668;
                clienteNDocumentoX = 122; clienteNDocumentoY = 645;
                clienteDireccionX = 150; clienteDireccionY = 622;
                fechaX = 120; fechaY = 691;
                sistemaX = 225; sistemaY = 453;
                tipoMantenimientoX = 235; tipoMantenimientoY = 485;
                fechaProgramadaX = 240; fechaProgramadaY = 517;
                trabajosRealizadosX = 130; trabajosRealizadosY = 545;
                yPositionDetalles = 500;
                subtotalX = 400; subtotalY = 350;
                impuestosX = 400; impuestosY = 320;
                totalX = 400; totalY = 285;
            }
            else // Tipo es "Factura"
            {
                // **ESTAS COORDENADAS SON EJEMPLOS. DEBES OBTENER LAS CORRECTAS PARA TU PLANTILLA DE FACTURA**
                numeroComprobanteX = 510; numeroComprobanteY = 784;
                clienteNombreX = 130; clienteNombreY = 669;
                clienteNDocumentoX = 107; clienteNDocumentoY = 646;
                clienteDireccionX = 152; clienteDireccionY = 625;
                fechaX = 123; fechaY = 692;
                sistemaX = 222; sistemaY = 454; // Corregir estas
                tipoMantenimientoX = 238; tipoMantenimientoY = 485; // Corregir estas
                fechaProgramadaX = 243; fechaProgramadaY = 517; // Corregir estas
                trabajosRealizadosX = 130; trabajosRealizadosY = 495; // Corregir estas
                yPositionDetalles = 500; // Corregir si es necesario
                subtotalX = 400; subtotalY = 350;
                impuestosX = 400; impuestosY = 320;
                totalX = 400; totalY = 285;
            }

            // Llenar el PDF con los datos usando las coordenadas asignadas
            document.ShowTextAligned(
                new Paragraph($"{comprobante.NumeroFactura ?? "N/A"}")
                    .SetFont(boldFont)
                    .SetFontSize(13), // <-- Agrega esta línea con el tamaño deseado (ej. 14)
                numeroComprobanteX, numeroComprobanteY, TextAlignment.LEFT);

            // Cliente
            document.ShowTextAligned(new Paragraph($"{cliente.Nombre}"),
                clienteNombreX, clienteNombreY, TextAlignment.LEFT);

            document.ShowTextAligned(new Paragraph($"{cliente.NDocumento ?? "N/A"}"),
                clienteNDocumentoX, clienteNDocumentoY, TextAlignment.LEFT);

            document.ShowTextAligned(new Paragraph($"{cliente.Direccion ?? "No registrada"}"),
                clienteDireccionX, clienteDireccionY, TextAlignment.LEFT);

            // Fecha
            document.ShowTextAligned(new Paragraph($"{comprobante.Fecha:dd/MM/yyyy}"),
                fechaX, fechaY, TextAlignment.LEFT);

            // Detalles del Mantenimiento
            if (comprobante.IdMantenimientoNavigation != null)
            {
                string nombreSistema = comprobante.IdMantenimientoNavigation.SistemaRenovable?.Descripcion ?? "No registrado";
                document.ShowTextAligned(new Paragraph($"{nombreSistema}"),
                    sistemaX, sistemaY, TextAlignment.LEFT);

                string tipoMantenimiento = comprobante.IdMantenimientoNavigation.TipoMantenimiento ?? "N/A";
                document.ShowTextAligned(new Paragraph($"{tipoMantenimiento}"),
                    tipoMantenimientoX, tipoMantenimientoY, TextAlignment.LEFT);

                document.ShowTextAligned(new Paragraph($"{comprobante.IdMantenimientoNavigation.FechaProgramada:dd/MM/yyyy}"),
                    fechaProgramadaX, fechaProgramadaY, TextAlignment.LEFT);

                //string trabajosRealizados = comprobante.IdMantenimientoNavigation.TrabajosRealizados ?? "No especificado";
                //document.ShowTextAligned(new Paragraph($"Trabajos Realizados: {trabajosRealizados}"),
                //    trabajosRealizadosX, trabajosRealizadosY, TextAlignment.LEFT);
            }

            // Detalles de los Productos (DetalleComprobante)
            if (comprobante.DetalleComprobantes != null && comprobante.DetalleComprobantes.Any())
            {
                float yPosition = yPositionDetalles;
                foreach (var detalle in comprobante.DetalleComprobantes)
                {
                    document.ShowTextAligned(new Paragraph($"{detalle.Cantidad}"),
                        100, yPosition, TextAlignment.RIGHT);
                    document.ShowTextAligned(new Paragraph($"{detalle.Concepto}"),
                        150, yPosition, TextAlignment.LEFT);
                    document.ShowTextAligned(new Paragraph($"{detalle.PrecioUnitario:N2}"),
                        300, yPosition, TextAlignment.RIGHT);
                    document.ShowTextAligned(new Paragraph($"{detalle.Importe:N2}"),
                        400, yPosition, TextAlignment.RIGHT);

                    yPosition -= 15;
                }
            }

            // Montos
            document.ShowTextAligned(new Paragraph($"{comprobante.Subtotal:N2}"),
                subtotalX, subtotalY, TextAlignment.LEFT);

            document.ShowTextAligned(new Paragraph($"{comprobante.Impuestos:N2}"),
                impuestosX, impuestosY, TextAlignment.LEFT);

            document.ShowTextAligned(new Paragraph($"{comprobante.Total:N2}"),
                totalX, totalY, TextAlignment.LEFT);

            document.Close();

            var pdfBytes = msOutput.ToArray();
            return File(pdfBytes, "application/pdf");
        }
        public async Task<IActionResult> GenerarComprobante(int id, string tipoComprobante)
        {
            var mantenimiento = await _context.Mantenimientos
                .Include(m => m.Cliente)
                .Include(m => m.Detalles)
                .FirstOrDefaultAsync(m => m.IdMantenimiento == id);

            if (mantenimiento == null)
                return NotFound();

            if (mantenimiento.Estado != "Finalizado")
                return BadRequest("Solo se pueden generar comprobantes para mantenimientos finalizados.");

            decimal subtotal = mantenimiento.CostoMantenimiento ?? 0m;
            subtotal += mantenimiento.Detalles?.Sum(d => d.SubTotal) ?? 0m;

            decimal impuestos = subtotal * 0.18m;
            decimal total = subtotal + impuestos;

            var comprobante = new Comprobante
            {
                IdCliente = mantenimiento.IdCliente,
                Tipo = string.IsNullOrWhiteSpace(tipoComprobante) ? "Factura" : tipoComprobante,
                Fecha = DateTime.Now,
                Subtotal = subtotal,
                Impuestos = impuestos,
                Total = total,
                IdMantenimiento = mantenimiento.IdMantenimiento,
                NumeroFactura = GenerarNumeroFactura() // <-- Aquí generamos el número automáticamente
            };

            _context.Comprobantes.Add(comprobante);
            await _context.SaveChangesAsync();

            return RedirectToAction("DetallesComprobante", new { id = comprobante.IdComprobante });
        }


        public async Task<IActionResult> DetallesComprobante(int id)
        {
            var comprobante = await _context.Comprobantes
                .Include(c => c.IdClienteNavigation)
                .Include(c => c.DetalleComprobantes)
                .FirstOrDefaultAsync(c => c.IdComprobante == id);

            if (comprobante == null)
                return NotFound();

            return View(comprobante);
        }

        [HttpGet]
        public async Task<IActionResult> VistaPreviaComprobante(int id)
        {
            var comprobante = await _context.Comprobantes
                .Include(c => c.IdClienteNavigation)
                .Include(c => c.DetalleComprobantes)
                .FirstOrDefaultAsync(c => c.IdComprobante == id);

            if (comprobante == null)
                return NotFound();

            ViewData["DisableLayout"] = true; // No usar layout completo para parcial
            ViewData["Preview"] = true;       // Puedes usar para condicionales en la vista

            return PartialView("DetallesComprobante", comprobante);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerComprobantePorMantenimiento(int id)
        {
            try
            {
                var comprobante = await _context.Comprobantes
                    .Where(c => c.IdMantenimiento == id)
                    .FirstOrDefaultAsync();

                if (comprobante == null)
                {
                    return Json(new { success = false, idComprobante = 0 });
                }

                return Json(new { success = true, idComprobante = comprobante.IdComprobante });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        private string GenerarNumeroFactura()
        {
            var ahora = DateTime.Now;
            string anio = ahora.Year.ToString();  // "2025"
            string mes = ahora.ToString("MM");    // "09"

            Console.WriteLine($"Generando número de factura para el año {anio} y mes {mes}");

            // Buscar facturas que correspondan al año y mes actuales
            var numerosFactura = _context.Comprobantes
                .Where(c => c.Tipo == "Factura"
                    && c.NumeroFactura != null
                    && c.NumeroFactura.StartsWith(mes)  // Que el número empiece con el mes
                    && c.Fecha.HasValue
                    && c.Fecha.Value.Year == ahora.Year  // Mismo año
                    && c.Fecha.Value.Month == ahora.Month) // Mismo mes
                .Select(c => c.NumeroFactura)
                .ToList();

            int maxNumero = 0;
            foreach (var numero in numerosFactura)
            {
                Console.WriteLine($"Analizando número de factura: {numero}");

                var partes = numero.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int n))
                {
                    Console.WriteLine($"Número extraído: {n}");

                    if (n > maxNumero)
                    {
                        maxNumero = n;
                        Console.WriteLine($"Nuevo máximo número encontrado: {maxNumero}");
                    }
                }
                else
                {
                    Console.WriteLine($"Formato inválido para número de factura: {numero}");
                }
            }

            int nuevoNumero = maxNumero + 1;
            string resultado = $"{mes}-{nuevoNumero:D4}";

            Console.WriteLine($"Número de factura generado: {resultado}");
            return resultado;
        }






        [HttpGet]
        public async Task<IActionResult> ObtenerSistemasPorCliente(int idCliente)
        {
            var sistemas = await _context.SistemasRenovables
                .Where(s => s.IdCliente == idCliente)
                .Select(s => new
                {
                    idSistema = s.IdSistema,
                    descripcion = s.Descripcion
                })
                .ToListAsync();

            return Json(sistemas);
        }
        // GET: Details
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var mantenimiento = await _context.Mantenimientos
                .Include(m => m.Cliente)
                .Include(m => m.SistemaRenovable)
                    .ThenInclude(s => s.IdTipoSistemaNavigation)
                .Include(m => m.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(m => m.IdMantenimiento == id.Value);

            if (mantenimiento == null) return NotFound();

            return View(mantenimiento);
        }


        // GET: Create (carga clientes y deja sistemas vacíos; sistemas se obtienen por AJAX)

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var clientesConSistemas = await _context.Clientes
                .Where(c => _context.SistemasRenovables.Any(sr => sr.IdCliente == c.IdCliente))
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var vm = new MantenimientoViewModel
            {
                FechaSolicitud = DateTime.Now,
                Estado = "Pendiente",
                Clientes = clientesConSistemas,  // <-- Aquí asignas solo esos clientes
                Sistemas = new List<SistemasRenovable>(),
                Productos = await _context.Productos
                                .Where(p => p.Activo.HasValue && p.Activo.Value)
                                .OrderBy(p => p.Nombre)
                                .ToListAsync(),
                Detalles = new List<DetalleMantenimientoViewModel>()
            };

            foreach (var p in vm.Productos)
            {
                vm.Detalles.Add(new DetalleMantenimientoViewModel
                {
                    IdProducto = p.IdProducto,
                    NombreProducto = p.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = p.PrecioVenta,
                    Seleccionado = false
                });
            }

            ViewBag.TiposMantenimiento = new[] { "Preventivo", "Correctivo" };
            ViewBag.Estados = new[] { "Pendiente", "En Proceso", "Finalizado" };

          

            return View(vm);
        }

        //// POST: Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MantenimientoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values)
                {
                    foreach (var err in error.Errors)
                    {
                        Console.WriteLine($"Error en el modelo: {err.ErrorMessage}");
                    }
                }

                vm.Clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
                vm.Sistemas = vm.IdCliente > 0
                    ? await _context.SistemasRenovables.Where(s => s.IdCliente == vm.IdCliente).OrderBy(s => s.Descripcion).ToListAsync()
                    : new List<SistemasRenovable>();
                vm.Productos = await _context.Productos
                                        .Where(p => p.Activo.HasValue && p.Activo.Value)
                                        .OrderBy(p => p.Nombre)
                                        .ToListAsync();

                return View(vm);
            }

            var existeSistema = await _context.SistemasRenovables.AnyAsync(s => s.IdSistema == vm.IdSistema);
            if (!existeSistema)
            {
                ModelState.AddModelError("IdSistema", "El sistema seleccionado no existe.");
                return View(vm);
            }

            decimal totalProductos = vm.Detalles?.Where(d => d.Seleccionado)
                                                 .Sum(d => d.Cantidad * d.PrecioUnitario) ?? 0m;

            var mantenimiento = new Mantenimiento
            {
                IdCliente = vm.IdCliente,
                IdSistema = vm.IdSistema,
                TipoMantenimiento = vm.TipoMantenimiento,
                FechaSolicitud = vm.FechaSolicitud,
                FechaProgramada = vm.FechaProgramada,
                FechaFin = vm.FechaFin,
                Estado = string.IsNullOrWhiteSpace(vm.Estado) ? "Pendiente" : vm.Estado,
                CostoMantenimiento = vm.CostoMantenimiento,
                CostoTotal = (vm.CostoMantenimiento ?? 0m) + totalProductos,
                RequiereProductos = vm.RequiereProductos,
                Diagnostico = vm.Diagnostico,
                Observaciones = vm.Observaciones,
                
            };

            if (vm.Detalles != null && vm.Detalles.Any())
            {
                mantenimiento.Detalles = vm.Detalles
                    .Where(d => d.Seleccionado)
                    .Select(d => new DetalleMantenimiento
                    {
                        IdProducto = d.IdProducto,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        SubTotal = d.Cantidad * d.PrecioUnitario
                    }).ToList();
            }

            _context.Mantenimientos.Add(mantenimiento);
            await _context.SaveChangesAsync();

            // ✅ Si el estado es "Finalizado", generar comprobante automáticamente
            
            
            decimal subtotal = mantenimiento.CostoMantenimiento ?? 0m;
            subtotal += mantenimiento.Detalles?.Sum(d => d.SubTotal) ?? 0m;
            decimal impuestos = subtotal * 0.18m;
            decimal total = subtotal + impuestos;

            var comprobante = new Comprobante
            {
                IdCliente = mantenimiento.IdCliente,
                Tipo = string.IsNullOrWhiteSpace(vm.TipoComprobante) ? "Factura" : vm.TipoComprobante,
                Fecha = DateTime.Now,
                Subtotal = subtotal,
                Impuestos = impuestos,
                Total = total,
                IdMantenimiento = mantenimiento.IdMantenimiento,
                NumeroFactura = GenerarNumeroFactura()  // <--- Aquí asignas el número generado
            };

            _context.Comprobantes.Add(comprobante);
            await _context.SaveChangesAsync();
            



            return RedirectToAction(nameof(Index));
        }



        // POST: Edit

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var mantenimiento = await _context.Mantenimientos
                .Include(m => m.Cliente)
                .Include(m => m.SistemaRenovable)
                .Include(m => m.Comprobantes) // ✅ Paso 1: Incluir la tabla de Comprobantes
                .Where(m => m.IdMantenimiento == id)
                .FirstOrDefaultAsync();

            if (mantenimiento == null)
                return NotFound();

            // ✅ Paso 2: Obtener el tipo de comprobante
            var tipoComprobante = mantenimiento.Comprobantes.FirstOrDefault()?.Tipo;

            var vm = new MantenimientoViewModel
            {
                IdMantenimiento = mantenimiento.IdMantenimiento,
                IdCliente = mantenimiento.IdCliente,
                IdSistema = mantenimiento.IdSistema,
                TipoMantenimiento = mantenimiento.TipoMantenimiento,
                FechaProgramada = mantenimiento.FechaProgramada,
                CostoMantenimiento = mantenimiento.CostoMantenimiento,
                Estado = mantenimiento.Estado,
                Diagnostico = mantenimiento.Diagnostico,
                Observaciones = mantenimiento.Observaciones,
                TipoComprobante = tipoComprobante, // ✅ Paso 3: Asignar el valor al ViewModel

                Clientes = await _context.Clientes
                    .Where(c => _context.SistemasRenovables.Any(s => s.IdCliente == c.IdCliente))
                    .OrderBy(c => c.Nombre)
                    .ToListAsync(),

                Sistemas = await _context.SistemasRenovables.Where(s => s.IdCliente == mantenimiento.IdCliente).ToListAsync()
            };

            ViewBag.Tipos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Preventivo", Text = "Preventivo", Selected = (vm.TipoMantenimiento == "Preventivo") },
                new SelectListItem { Value = "Correctivo", Text = "Correctivo", Selected = (vm.TipoMantenimiento == "Correctivo") }
            };
                    ViewBag.Estados = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente", Selected = (vm.Estado == "Pendiente") },
                new SelectListItem { Value = "En Proceso", Text = "En Proceso", Selected = (vm.Estado == "En Proceso") },
                new SelectListItem { Value = "Finalizado", Text = "Finalizado", Selected = (vm.Estado == "Finalizado") }
            };
                    // ✅ Paso 4: Crear un ViewBag para el tipo de comprobante
                    ViewBag.TiposComprobante = new List<SelectListItem>
            {
                new SelectListItem { Value = "Factura", Text = "Factura", Selected = (vm.TipoComprobante == "Factura") },
                new SelectListItem { Value = "Boleta", Text = "Boleta", Selected = (vm.TipoComprobante == "Boleta") }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MantenimientoViewModel vm)
        {
            if (id != vm.IdMantenimiento)
                return NotFound();

            if (!ModelState.IsValid)
            {
                // Tu lógica para recargar el ViewModel y los ViewBags
                vm.Clientes = await _context.Clientes
                                            .Where(c => _context.SistemasRenovables.Any(s => s.IdCliente == c.IdCliente))
                                            .OrderBy(c => c.Nombre).ToListAsync();
                vm.Sistemas = await _context.SistemasRenovables
                                            .Where(s => s.IdCliente == vm.IdCliente)
                                            .OrderBy(s => s.Descripcion)
                                            .ToListAsync();

                // ¡Importante! Asegúrate de recargar los ViewBags
                ViewBag.Tipos = new List<SelectListItem>
        {
            new SelectListItem { Value = "Preventivo", Text = "Preventivo", Selected = (vm.TipoMantenimiento == "Preventivo") },
            new SelectListItem { Value = "Correctivo", Text = "Correctivo", Selected = (vm.TipoMantenimiento == "Correctivo") }
        };

                ViewBag.Estados = new List<SelectListItem>
        {
            new SelectListItem { Value = "Pendiente", Text = "Pendiente", Selected = (vm.Estado == "Pendiente") },
            new SelectListItem { Value = "En Proceso", Text = "En Proceso", Selected = (vm.Estado == "En Proceso") },
            new SelectListItem { Value = "Finalizado", Text = "Finalizado", Selected = (vm.Estado == "Finalizado") }
        };

                ViewBag.TiposComprobante = new List<SelectListItem>
        {
            new SelectListItem { Value = "Factura", Text = "Factura", Selected = (vm.TipoComprobante == "Factura") },
            new SelectListItem { Value = "Boleta", Text = "Boleta", Selected = (vm.TipoComprobante == "Boleta") }
        };

                return View(vm);
            }

            var mantenimiento = await _context.Mantenimientos
                .Include(m => m.Detalles)
                .Include(m => m.Comprobantes) // ✅ ¡IMPORTANTE! Vuelve a incluir la tabla de Comprobantes para poder encontrar uno existente.
                .FirstOrDefaultAsync(m => m.IdMantenimiento == id);

            if (mantenimiento == null)
                return NotFound();

            if (!_context.SistemasRenovables.Any(s => s.IdSistema == vm.IdSistema))
            {
                ModelState.AddModelError("IdSistema", "El sistema seleccionado no existe en la base de datos.");
                return View(vm);
            }

            decimal totalProductos = vm.Detalles?.Where(d => d.Seleccionado)
                                                 .Sum(d => d.Cantidad * d.PrecioUnitario) ?? 0m;

            // Actualizar propiedades del mantenimiento
            mantenimiento.IdCliente = vm.IdCliente;
            mantenimiento.IdSistema = vm.IdSistema;
            mantenimiento.TipoMantenimiento = vm.TipoMantenimiento;
            mantenimiento.FechaProgramada = vm.FechaProgramada;
            mantenimiento.Estado = vm.Estado;
            mantenimiento.CostoMantenimiento = vm.CostoMantenimiento;
            mantenimiento.CostoTotal = (vm.CostoMantenimiento ?? 0m) + totalProductos;
            mantenimiento.Diagnostico = vm.Diagnostico;
            mantenimiento.Observaciones = vm.Observaciones;

            // --- LÓGICA CORREGIDA PARA EL COMPROBANTE ---
            var comprobanteExistente = mantenimiento.Comprobantes.FirstOrDefault();

            // Recalcular montos
            decimal subtotal = mantenimiento.CostoMantenimiento ?? 0m;
            var detalles = mantenimiento.Detalles?.ToList() ?? new List<DetalleMantenimiento>();
            subtotal += detalles.Sum(d => d.SubTotal);
            decimal impuestos = subtotal * 0.18m;
            decimal total = subtotal + impuestos;

            // 1. Si el mantenimiento está FINALIZADO Y se ha seleccionado un tipo de comprobante
            if (mantenimiento.Estado == "Finalizado" && !string.IsNullOrWhiteSpace(vm.TipoComprobante))
            {
                // a) Si YA EXISTE un comprobante, lo actualizamos.
                if (comprobanteExistente != null)
                {
                    comprobanteExistente.Tipo = vm.TipoComprobante;
                    comprobanteExistente.Fecha = DateTime.Now;
                    comprobanteExistente.Subtotal = subtotal;
                    comprobanteExistente.Impuestos = impuestos;
                    comprobanteExistente.Total = total;

                    if (comprobanteExistente.Tipo == "Factura" && string.IsNullOrWhiteSpace(comprobanteExistente.NumeroFactura))
                    {
                        comprobanteExistente.NumeroFactura = GenerarNumeroFactura();
                    }
                    else if (comprobanteExistente.Tipo != "Factura")
                    {
                        comprobanteExistente.NumeroFactura = null;
                    }

                    _context.Comprobantes.Update(comprobanteExistente);
                }
                // b) Si NO EXISTE un comprobante, lo creamos.
                else
                {
                    var comprobante = new Comprobante
                    {
                        IdCliente = mantenimiento.IdCliente,
                        Tipo = vm.TipoComprobante, // ✅ Asignar el tipo del ViewModel
                        Fecha = DateTime.Now,
                        Subtotal = subtotal,
                        Impuestos = impuestos,
                        Total = total,
                        IdMantenimiento = mantenimiento.IdMantenimiento,
                        NumeroFactura = (vm.TipoComprobante == "Factura") ? GenerarNumeroFactura() : null
                    };

                    // Lógica para añadir los detalles de productos al nuevo comprobante
                    foreach (var detalle in detalles)
                    {
                        var producto = await _context.Productos.FindAsync(detalle.IdProducto);
                        comprobante.DetalleComprobantes.Add(new DetalleComprobante
                        {
                            IdProducto = detalle.IdProducto,
                            Concepto = producto?.Nombre ?? "Producto utilizado",
                            Cantidad = detalle.Cantidad,
                            PrecioUnitario = detalle.PrecioUnitario,
                            Importe = detalle.SubTotal
                        });
                    }

                    _context.Comprobantes.Add(comprobante);
                }
            }
            // 2. Si el mantenimiento NO está FINALIZADO pero tiene un comprobante (por error), lo eliminamos.
            else if (mantenimiento.Estado != "Finalizado" && comprobanteExistente != null)
            {
                _context.Comprobantes.Remove(comprobanteExistente);
            }

            _context.Mantenimientos.Update(mantenimiento);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Delete
        // GET: Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var mantenimiento = await _context.Mantenimientos
                .Include(m => m.Cliente)
                .Include(m => m.SistemaRenovable)
                .Include(m => m.Detalles)
                .FirstOrDefaultAsync(m => m.IdMantenimiento == id.Value);

            if (mantenimiento == null) return NotFound();

            return View(mantenimiento);
        }

        // POST: DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mantenimiento = await _context.Mantenimientos
                .FirstOrDefaultAsync(m => m.IdMantenimiento == id);

            if (mantenimiento == null)
            {
                return NotFound();
            }

            var ventaAsociada = await _context.Ventas
                .FirstOrDefaultAsync(v => v.IdMantenimiento == mantenimiento.IdMantenimiento);

            if (ventaAsociada != null)
            {
                TempData["Error"] = "No se puede eliminar este mantenimiento porque está relacionado con una venta. Elimine la venta primero.";
                return RedirectToAction("Index"); // Redirige al Index de Mantenimientos
            }

            try
            {
                var alertaAsociada = await _context.Alertas
                    .FirstOrDefaultAsync(a => a.IdSistema == mantenimiento.IdSistema);
                if (alertaAsociada != null)
                {
                    _context.Alertas.Remove(alertaAsociada);
                }

                if (mantenimiento.Detalles != null && mantenimiento.Detalles.Any())
                {
                    _context.DetalleMantenimientos.RemoveRange(mantenimiento.Detalles);
                }

                _context.Mantenimientos.Remove(mantenimiento);

                await _context.SaveChangesAsync();

                TempData["Ok"] = "El mantenimiento se ha eliminado correctamente."; // Opcional: mensaje de éxito
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Ocurrió un error al intentar eliminar el mantenimiento. Puede haber registros relacionados que impiden la eliminación.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
