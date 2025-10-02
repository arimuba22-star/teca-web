using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using SWebEnergia.Models.Metadata;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SWebEnergia.Controllers
{
    public class ClientesController : Controller
    {
        private readonly EnergiaContext _context;
        public ClientesController(EnergiaContext context) => _context = context;

        // GET: Clientes
        public async Task<IActionResult> Index(string? q, string? sort)
        {
            ViewData["CurrentFilter"] = q;
            ViewData["NombreSort"] = sort == "nombre_asc" ? "nombre_desc" : "nombre_asc";
            ViewData["EmailSort"] = sort == "email_asc" ? "email_desc" : "email_asc";

            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c =>
                    c.Nombre.Contains(q) ||
                    c.Email.Contains(q) ||
                    (c.Telefono != null && c.Telefono.Contains(q)) ||
                    (c.Direccion != null && c.Direccion.Contains(q)) ||
                    (c.TDocumento != null && c.TDocumento.Contains(q)) ||   // Buscar por tipo de documento
                    (c.NDocumento != null && c.NDocumento.Contains(q))      // Buscar por número de documento
                );
            }

            query = sort switch
            {
                "nombre_desc" => query.OrderByDescending(c => c.Nombre),
                "email_asc" => query.OrderBy(c => c.Email),
                "email_desc" => query.OrderByDescending(c => c.Email),
                _ => query.OrderBy(c => c.Nombre)
            };

            var lista = await query.AsNoTracking().ToListAsync();
            return View(lista);
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(m => m.IdCliente == id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // GET: Clientes/Create
        public IActionResult Create() => View();

        // POST: Clientes/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Email,Telefono,Direccion,TDocumento,NDocumento,FechaNacimiento")] Cliente cliente)
        {
            try
            {
                // Validación del teléfono (debe ser un número de 9 caracteres)
                if (cliente.Telefono != null && cliente.Telefono.Length != 9)
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener exactamente 9 caracteres.");
                }

                // Validar que NDocumento tenga como máximo 8 caracteres si TDocumento es "DNI"
                if (cliente.TDocumento == "DNI" && cliente.NDocumento != null && cliente.NDocumento.Length > 8)
                {
                    ModelState.AddModelError("NDocumento", "El número de DNI no puede tener más de 8 caracteres.");
                }
                // Validar que NDocumento tenga como máximo 11 caracteres si TDocumento es "RUC" o "CARNET DE EXTRANJERIA"
                else if ((cliente.TDocumento == "RUC" || cliente.TDocumento == "CARNET DE EXTRANJERIA") &&
                         cliente.NDocumento != null && cliente.NDocumento.Length > 11)
                {
                    ModelState.AddModelError("NDocumento", "El número de documento no puede tener más de 11 caracteres.");
                }

                // Validar que el email contenga el símbolo '@'
                if (string.IsNullOrEmpty(cliente.Email) || !cliente.Email.Contains('@'))
                {
                    ModelState.AddModelError("Email", "El correo electrónico debe contener '@'.");
                }

                if (!ModelState.IsValid)
                {
                    return View(cliente);
                }

                _context.Add(cliente);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Cliente creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Capturar cualquier error inesperado
                TempData["Error"] = "Ocurrió un error al intentar crear el cliente: " + ex.Message;
                return View(cliente);
            }
        }




        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCliente,Nombre,Email,Telefono,Direccion,TDocumento,NDocumento,FechaNacimiento")] Cliente cliente)
        {
            if (id != cliente.IdCliente)
            {
                return NotFound();
            }

            try
            {
                // Validar que NDocumento tenga como máximo 8 caracteres si TDocumento es "DNI"
                if (cliente.TDocumento == "DNI" && cliente.NDocumento != null && cliente.NDocumento.Length > 8)
                {
                    ModelState.AddModelError("NDocumento", "El número de DNI no puede tener más de 8 caracteres.");
                }

                // Validar que NDocumento tenga como máximo 11 caracteres si TDocumento es "RUC" o "CARNET DE EXTRANJERIA"
                else if ((cliente.TDocumento == "RUC" || cliente.TDocumento == "CARNET DE EXTRANJERIA") &&
                         cliente.NDocumento != null && cliente.NDocumento.Length > 11)
                {
                    ModelState.AddModelError("NDocumento", "El número de documento no puede tener más de 11 caracteres.");
                }

                // Validar que el email contenga el símbolo '@'
                if (string.IsNullOrEmpty(cliente.Email) || !cliente.Email.Contains('@'))
                {
                    ModelState.AddModelError("Email", "El correo electrónico debe contener '@'.");
                }

                // Validar que el teléfono tenga exactamente 9 caracteres (solo números)
                if (cliente.Telefono != null && cliente.Telefono.Length != 9)
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener exactamente 9 caracteres.");
                }

                if (!ModelState.IsValid)
                {
                    return View(cliente);
                }

                _context.Update(cliente);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Cliente actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Capturar errores relacionados con la concurrencia de la base de datos
                TempData["Error"] = "Ocurrió un error al intentar actualizar el cliente: " + ex.Message;
                if (!await _context.Clientes.AnyAsync(e => e.IdCliente == id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                // Capturar otros errores inesperados
                TempData["Error"] = "Ocurrió un error inesperado: " + ex.Message;
                return View(cliente);
            }
        }



        // GET: Clientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(m => m.IdCliente == id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente != null)
                {
                    _context.Clientes.Remove(cliente);
                    await _context.SaveChangesAsync();
                    TempData["Ok"] = "Cliente eliminado.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Capturar cualquier error inesperado al eliminar el cliente
                TempData["Error"] = "Ocurrió un error al intentar eliminar el cliente: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
