using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using SWebEnergia.Services;
using System;

namespace SWebEnergia.Controllers
{
    public class SistemasRenovablesController : Controller
    {
        private readonly EnergiaContext _context;
        private readonly EmailService _emailService;
        // 🔽 Agrega esto justo aquí
        private static readonly Dictionary<string, List<string>> DepartamentosProvincias = new()
        {
            { "Amazonas", new List<string> { "Chachapoyas", "Bagua", "Bongará", "Condorcanqui", "Luya", "Rodríguez de Mendoza", "Utcubamba" } },
            { "Áncash", new List<string> { "Huaraz", "Aija", "Antonio Raymondi", "Asunción", "Bolognesi", "Carhuaz", "Carlos Fermín Fitzcarrald", "Casma", "Corongo", "Huari", "Huarmey", "Huaylas", "Mariscal Luzuriaga", "Ocros", "Pallasca", "Pomabamba", "Recuay", "Santa", "Sihuas", "Yungay" } },
            { "Apurímac", new List<string> { "Abancay", "Andahuaylas", "Antabamba", "Aymaraes", "Cotabambas", "Chincheros", "Grau" } },
            { "Arequipa", new List<string> { "Arequipa", "Camaná", "Caravelí", "Castilla", "Caylloma", "Condesuyos", "Islay", "La Unión" } },
            { "Ayacucho", new List<string> { "Huamanga", "Cangallo", "Huanca Sancos", "Huanta", "La Mar", "Lucanas", "Parinacochas", "Páucar del Sara Sara", "Sucre", "Víctor Fajardo", "Vilcas Huamán" } },
            { "Cajamarca", new List<string> { "Cajamarca", "Cajabamba", "Celendín", "Chota", "Contumazá", "Cutervo", "Hualgayoc", "Jaén", "San Ignacio", "San Marcos", "San Miguel", "San Pablo", "Santa Cruz" } },
            { "Callao", new List<string> { "Callao" } },
            { "Cusco", new List<string> { "Cusco", "Acomayo", "Anta", "Calca", "Canas", "Canchis", "Chumbivilcas", "Espinar", "La Convención", "Paruro", "Paucartambo", "Quispicanchi", "Urubamba" } },
            { "Huancavelica", new List<string> { "Huancavelica", "Acobamba", "Angaraes", "Castrovirreyna", "Churcampa", "Huaytará", "Tayacaja" } },
            { "Huánuco", new List<string> { "Huánuco", "Ambo", "Dos de Mayo", "Huacaybamba", "Huamalíes", "Leoncio Prado", "Marañón", "Pachitea", "Puerto Inca", "Lauricocha", "Yarowilca" } },
            { "Ica", new List<string> { "Ica", "Chincha", "Nazca", "Palpa", "Pisco" } },
            { "Junín", new List<string> { "Huancayo", "Chanchamayo", "Chupaca", "Concepción", "Jauja", "Junín", "Satipo", "Tarma", "Yauli" } },
            { "La Libertad", new List<string> { "Trujillo", "Ascope", "Bolívar", "Chepén", "Gran Chimú", "Julcán", "Otuzco", "Pacasmayo", "Pataz", "Sánchez Carrión", "Santiago de Chuco", "Virú" } },
            { "Lambayeque", new List<string> { "Chiclayo", "Ferreñafe", "Lambayeque" } },
            { "Lima", new List<string> { "Lima", "Barranca", "Cajatambo", "Canta", "Cañete", "Huaral", "Huarochirí", "Huaura", "Oyón", "Yauyos" } },
            { "Loreto", new List<string> { "Maynas", "Alto Amazonas", "Datem del Marañón", "Loreto", "Mariscal Ramón Castilla", "Putumayo", "Requena", "Ucayali" } },
            { "Madre de Dios", new List<string> { "Tambopata", "Manu", "Tahuamanu" } },
            { "Moquegua", new List<string> { "Mariscal Nieto", "General Sánchez Cerro", "Ilo" } },
            { "Pasco", new List<string> { "Pasco", "Daniel Alcides Carrión", "Oxapampa" } },
            { "Piura", new List<string> { "Piura", "Ayabaca", "Huancabamba", "Morropón", "Paita", "Sullana", "Talara", "Sechura" } },
            { "Puno", new List<string> { "Puno", "Azángaro", "Carabaya", "Chucuito", "El Collao", "Huancané", "Lampa", "Melgar", "Moho", "San Antonio de Putina", "San Román", "Sandia", "Yunguyo" } },
            { "San Martín", new List<string> { "Moyobamba", "Bellavista", "El Dorado", "Huallaga", "Lamas", "Mariscal Cáceres", "Picota", "Rioja", "San Martín", "Tocache" } },
            { "Tacna", new List<string> { "Tacna", "Candarave", "Jorge Basadre", "Tarata" } },
            { "Tumbes", new List<string> { "Tumbes", "Contralmirante Villar", "Zarumilla" } },
            { "Ucayali", new List<string> { "Coronel Portillo", "Atalaya", "Padre Abad", "Purús" } }
        };

        public SistemasRenovablesController(EnergiaContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: SistemasRenovables
        public async Task<IActionResult> Index(string? q, int? clienteId)
        {
            ViewData["CurrentFilter"] = q;
            ViewData["CurrentCliente"] = clienteId;

            ViewBag.Clientes = new SelectList(_context.Clientes.OrderBy(c => c.Nombre), "IdCliente", "Nombre");

            var query = _context.SistemasRenovables
                .Include(s => s.IdClienteNavigation)
                .Include(s => s.IdTipoSistemaNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(s =>
                    s.Descripcion!.Contains(q) ||
                    s.Estado!.Contains(q) ||
                    s.Departamento!.Contains(q) ||
                    s.Provincia!.Contains(q) ||
                    s.IdClienteNavigation!.Nombre.Contains(q));
            }



            if (clienteId.HasValue)
            {
                query = query.Where(s => s.IdCliente == clienteId.Value);
            }

            var lista = await query.AsNoTracking().ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public JsonResult ObtenerProvincias(string departamento)
        {
            if (string.IsNullOrWhiteSpace(departamento) || !DepartamentosProvincias.ContainsKey(departamento))
            {
                return Json(new List<string>());
            }

            var provincias = DepartamentosProvincias[departamento];
            return Json(provincias);
        }


        // GET: SistemasRenovables/Details/5
        // GET: SistemasRenovables/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sistema = await _context.SistemasRenovables
                .Include(s => s.IdClienteNavigation)
                .Include(s => s.IdTipoSistemaNavigation)
                .Include(s => s.Componentes)
                    .ThenInclude(c => c.TipoComponente) // Para mostrar nombre del tipo
                .FirstOrDefaultAsync(m => m.IdSistema == id);

            if (sistema == null) return NotFound();

            return View(sistema);
        }

        // ✅ Método privado para evitar repetir código
        private void CargarViewBags(SistemasRenovable sistema = null)
        {
            ViewBag.Clientes = new SelectList(_context.Clientes, "IdCliente", "Nombre", sistema?.IdCliente);
            ViewBag.TiposSistema = new SelectList(_context.TiposSistemas, "IdTipoSistema", "Nombre", sistema?.IdTipoSistema);
            ViewBag.TiposComponentes = new SelectList(_context.TipoComponentes, "IdTipoComponente", "Descripcion");
            ViewBag.Estados = new List<SelectListItem>
            {
                new SelectListItem { Value = "Activo", Text = "Activo" },
                new SelectListItem { Value = "Inactivo", Text = "Inactivo" }
            };
        }



        // GET: SistemasRenovables/Create
        // GET: SistemasRenovables/Create
        public IActionResult Create()
        {
            ViewBag.Clientes = new SelectList(_context.Clientes, "IdCliente", "Nombre");
            ViewBag.TiposSistema = new SelectList(_context.TiposSistemas, "IdTipoSistema", "Nombre");
            ViewBag.TiposComponentes = new SelectList(_context.TipoComponentes, "IdTipoComponente", "Descripcion");

            ViewBag.Estados = new List<SelectListItem>
            {
                new SelectListItem { Value = "Activo", Text = "Activo" },
                new SelectListItem { Value = "Inactivo", Text = "Inactivo" }
            };

            return View();
        }


        // POST: SistemasRenovables/Create
        // POST: SistemasRenovables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SistemasRenovable sistema, List<Componente> componentes)
        {
            try
            {
                Console.WriteLine("========= [Create POST] =========");
                Console.WriteLine($"Sistema recibido: {sistema.Descripcion}, Cliente ID: {sistema.IdCliente}, Tipo Sistema ID: {sistema.IdTipoSistema}");
                Console.WriteLine($"Total componentes recibidos: {componentes.Count}");

                // ✳️ Evita duplicados por model binding automático
                sistema.Componentes = null;

                if (ModelState.IsValid)
                {
                    // 1. Guardar sistema
                    _context.SistemasRenovables.Add(sistema);
                    await _context.SaveChangesAsync(); // Ya tiene IdSistema

                    // 2. Eliminar duplicados en memoria
                    var componentesUnicos = componentes
                        .GroupBy(c => new { c.Descripcion, c.IdTipoComponente })
                        .Select(g => g.First())
                        .ToList();

                    // 3. Verificar si ya existen en base de datos (Opcional, pero se mantiene la lógica)
                    var existentes = await _context.Componentes
                        .AsNoTracking()
                        .Where(c => c.IdSistema == sistema.IdSistema)
                        .ToListAsync();

                    var componentesNuevos = componentesUnicos
                        .Where(c => !existentes.Any(e =>
                            e.Descripcion == c.Descripcion &&
                            e.IdTipoComponente == c.IdTipoComponente))
                        .ToList();

                    Console.WriteLine($"Componentes nuevos a insertar: {componentesNuevos.Count}");

                    // 4. Insertar solo los nuevos
                    foreach (var comp in componentesNuevos)
                    {
                        comp.IdSistema = sistema.IdSistema;
                        _context.Componentes.Add(comp);
                        Console.WriteLine($" --> Guardando componente: {comp.Descripcion} - Tipo: {comp.IdTipoComponente}");
                    }

                    await _context.SaveChangesAsync();

                    // 5. Cargar datos de navegación para el correo
                    var cliente = await _context.Clientes.FindAsync(sistema.IdCliente);
                    // Cargar el Tipo de Sistema y el Cliente (necesario para el correo del dueño)
                    await _context.Entry(sistema).Reference(s => s.IdTipoSistemaNavigation).LoadAsync();
                    await _context.Entry(sistema).Reference(s => s.IdClienteNavigation).LoadAsync(); // 🛑 NUEVO: Carga el objeto Cliente completo

                    // 🛑 INICIO: Carga de componentes para el Email 🛑

                    // 6. Cargar la colección de componentes en el objeto 'sistema'
                    await _context.Entry(sistema).Collection(s => s.Componentes).LoadAsync();

                    // 7. Cargar la relación 'TipoComponente' para cada componente agregado
                    if (sistema.Componentes != null)
                    {
                        foreach (var comp in sistema.Componentes)
                        {
                            await _context.Entry(comp).Reference(c => c.TipoComponente).LoadAsync();
                        }
                    }

                    // 🛑 FIN: Carga de componentes para el Email 🛑

                    // 8. Configuración y ENVÍO DOBLE DEL CORREO
                    string correoDueno = "tecnoelectricaindustrialandino@gmail.com";
                    string nombreDueno = "Administrador";

                    if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Email))
                    {
                        // A. Enviar al Cliente
                        await _emailService.EnviarCorreoNotificacionAsync(cliente.Email, cliente.Nombre, sistema, esDueno: false);
                        Console.WriteLine($"📧 Correo enviado a {cliente.Email}");
                    }

                    // B. Enviar al Dueño/Administrador (Siempre)
                    await _emailService.EnviarCorreoNotificacionAsync(correoDueno, nombreDueno, sistema, esDueno: true);
                    Console.WriteLine($"📧 Correo enviado al administrador: {correoDueno}");


                    TempData["Ok"] = "Sistema y componentes registrados correctamente.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                TempData["Error"] = $"Error al registrar: {ex.Message}";
            }

            // Si falla, recargar ViewBag y volver a la vista
            ViewBag.Clientes = new SelectList(_context.Clientes, "IdCliente", "Nombre", sistema.IdCliente);
            ViewBag.TiposSistema = new SelectList(_context.TiposSistemas, "IdTipoSistema", "Nombre", sistema.IdTipoSistema);
            ViewBag.TiposComponentes = new SelectList(_context.TipoComponentes, "IdTipoComponente", "Descripcion");
            ViewBag.Estados = new List<SelectListItem>
            {
                new SelectListItem { Value = "Activo", Text = "Activo" },
                new SelectListItem { Value = "Inactivo", Text = "Inactivo" }
            };

            return View(sistema);
        }


        // GET: SistemasRenovables/Edit/5
        // GET: SistemasRenovables/Edit/5
        // GET: SistemasRenovables/Edit/5
        // GET: SistemasRenovables/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sistema = await _context.SistemasRenovables
                .AsNoTracking() // ✅ Evita conflictos de tracking
                .Include(s => s.IdClienteNavigation)
                .Include(s => s.IdTipoSistemaNavigation)
                .Include(s => s.Componentes)
                .FirstOrDefaultAsync(s => s.IdSistema == id);

            if (sistema == null) return NotFound();

            CargarViewBags(sistema); // ✅ Cargar combos
            return View(sistema);
        }




        // POST: SistemasRenovables/Edit/5
        // POST: SistemasRenovables/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SistemasRenovable sistema, List<Componente> componentes)
        {
            if (id != sistema.IdSistema)
                return NotFound();

            if (!ModelState.IsValid)
            {
                CargarViewBags(sistema);
                return View(sistema);
            }

            try
            {
                // Actualizar el sistema sin componentes para evitar conflicto de tracking
                _context.Entry(sistema).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Obtener componentes actuales en la BD
                var existentes = await _context.Componentes
                    .Where(c => c.IdSistema == sistema.IdSistema)
                    .ToListAsync();

                var idsFormulario = componentes
                    .Where(c => c.IdComponente.HasValue)
                    .Select(c => c.IdComponente.Value)
                    .ToList();

                // Eliminar componentes que ya no están en el formulario
                var aEliminar = existentes
                    .Where(c => !idsFormulario.Contains(c.IdComponente.Value))
                    .ToList();

                _context.Componentes.RemoveRange(aEliminar);

                // Agregar o actualizar componentes
                foreach (var comp in componentes)
                {
                    comp.IdSistema = sistema.IdSistema;

                    if (comp.IdComponente.HasValue && comp.IdComponente > 0)
                    {
                        // Buscar el componente existente para actualizarlo
                        var existente = existentes
                            .FirstOrDefault(c => c.IdComponente == comp.IdComponente.Value);

                        if (existente != null)
                        {
                            existente.Descripcion = comp.Descripcion;
                            existente.IdTipoComponente = comp.IdTipoComponente;
                            existente.CapacidadEnergetica = comp.CapacidadEnergetica;
                            existente.FechaInstalacion = comp.FechaInstalacion;
                            // Actualiza otras propiedades si tienes más
                        }
                    }
                    else
                    {
                        // Nuevo componente, agregarlo
                        _context.Componentes.Add(comp);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Ok"] = "Sistema y componentes actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
                CargarViewBags(sistema);
                return View(sistema);
            }
        }



        // GET: SistemasRenovables/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var sistema = await _context.SistemasRenovables
                .Include(s => s.IdClienteNavigation)
                .Include(s => s.IdTipoSistemaNavigation)
                .FirstOrDefaultAsync(m => m.IdSistema == id);

            if (sistema == null) return NotFound();

            return View(sistema);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                Console.WriteLine($"[LOG] Intentando eliminar sistema ID: {id}");

                var sistema = await _context.SistemasRenovables
                    .Include(s => s.Componentes)
                    .Include(s => s.Mantenimientos)
                    .Include(s => s.Alertas)
                    .FirstOrDefaultAsync(s => s.IdSistema == id);

                if (sistema == null)
                {
                    TempData["Error"] = "Sistema no encontrado.";
                    Console.WriteLine("[LOG] Sistema no encontrado.");
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"[LOG] Componentes: {sistema.Componentes.Count}, Mantenimientos: {sistema.Mantenimientos.Count}, Alertas: {sistema.Alertas.Count}");

                if (sistema.Componentes.Any())
                    _context.Componentes.RemoveRange(sistema.Componentes);

                if (sistema.Mantenimientos.Any())
                    _context.Mantenimientos.RemoveRange(sistema.Mantenimientos);

                if (sistema.Alertas.Any())
                    _context.Alerta.RemoveRange(sistema.Alertas);

                _context.SistemasRenovables.Remove(sistema);
                await _context.SaveChangesAsync();

                TempData["Ok"] = "Sistema eliminado correctamente.";
                Console.WriteLine("[LOG] Sistema eliminado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                TempData["Error"] = $"Error al eliminar el sistema: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
