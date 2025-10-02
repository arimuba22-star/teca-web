using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;

namespace SWebEnergia.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly EnergiaContext _context;
        private readonly IConfiguration _config;

        public UsuariosController(EnergiaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: UsuariosController
        public async Task<IActionResult> Index(string? q, string? sort)
        {
            ViewData["CurrentFilter"] = q;
            ViewData["NombreSort"] = sort == "nombre_asc" ? "nombre_desc" : "nombre_asc";
            ViewData["EmailSort"] = sort == "email_asc" ? "email_desc" : "email_asc";

            var query = _context.Usuarios.Include(u => u.IdRolNavigation).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(u =>
                    u.Nombre.Contains(q) ||
                    u.Email.Contains(q)
                );
            }

            query = sort switch
            {
                "nombre_desc" => query.OrderByDescending(u => u.Nombre),
                "email_asc" => query.OrderBy(u => u.Email),
                "email_desc" => query.OrderByDescending(u => u.Email),
                _ => query.OrderBy(u => u.Nombre)
            };

            var lista = await query.AsNoTracking().ToListAsync();
            return View(lista);
        }

        // GET: UsuariosController/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdUsuario == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }


        private string GenerarSalt()
        {
            byte[] saltBytes = new byte[16];
            System.Security.Cryptography.RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var combined = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(combined);
                return Convert.ToBase64String(hash);
            }
        }


        // GET: UsuariosController/Create
        public IActionResult Create()
        {
            var roles = _context.Roles.ToList();

            if (roles == null || !roles.Any())
            {
                TempData["Error"] = "No hay roles disponibles en la base de datos.";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = new SelectList(roles, "IdRol", "Nombre");
            return View();
        }

        // POST: UsuariosController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Email,PasswordHash,Activo,IdRol,FechaNacimiento")] Usuario usuario)
        {
            // ✅ Validaciones personalizadas
            if (Regex.IsMatch(usuario.Nombre, @"\d"))
                ModelState.AddModelError("Nombre", "El nombre no debe contener números.");

            if (string.IsNullOrWhiteSpace(usuario.Email) || !usuario.Email.Contains("@"))
                ModelState.AddModelError("Email", "El correo electrónico no es válido.");

            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                ModelState.AddModelError("Email", "El correo ya está registrado.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(_context.Roles, "IdRol", "Nombre");
                return View(usuario);
            }

            // Generar salt + hash
            usuario.Salt = GenerarSalt();
            string passwordBase = usuario.PasswordHash; // contraseña cruda
            usuario.PasswordHash = HashPassword(passwordBase, usuario.Salt);
            usuario.FechaCreacion = DateTime.Now;

            _context.Add(usuario);
            await _context.SaveChangesAsync();

            // ✅ Crear link de cambio de contraseña
            var changePasswordLink = Url.Action("ChangePassword", "Account", new { email = usuario.Email }, Request.Scheme);

            // ✅ Enviar correo al usuario con credenciales iniciales + link
            string cuerpo = $@"
                <p>Estimado <b>{usuario.Nombre}</b>,</p>
                <p>Se ha creado su cuenta en <b>Plataforma Web TECA</b>.</p>
                <p>Sus credenciales son:</p>
                <ul>
                    <li><b>Usuario:</b> {usuario.Email}</li>
                    <li><b>Contraseña temporal:</b> {passwordBase}</li>
                </ul>
                <p>Por motivos de seguridad, le recomendamos <b>cambiar su contraseña inmediatamente</b> usando el siguiente enlace:</p>
                <p><a href='{changePasswordLink}' style='color:#5A1C16;font-weight:bold;'>🔑 Cambiar mi contraseña</a></p>
                <br>
                <p>Atentamente,</p>
                <p><b>Soporte TECA</b></p>";

            EnviarCorreo(usuario.Email, "Credenciales de acceso - Plataforma Web TECA", cuerpo);

            TempData["Ok"] = "Usuario creado correctamente y correo enviado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: UsuariosController/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            ViewData["Roles"] = new SelectList(_context.Roles, "IdRol", "Nombre", usuario.IdRol);
            return View(usuario);
        }

        // POST: UsuariosController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Nombre,Email,Activo,IdRol,FechaNacimiento")] Usuario usuario)
        {
            if (id != usuario.IdUsuario) return NotFound();

            if (!ModelState.IsValid) return View(usuario);

            try
            {
                var usuarioOriginal = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
                if (usuarioOriginal == null) return NotFound();

                usuario.PasswordHash = usuarioOriginal.PasswordHash;
                usuario.Salt = usuarioOriginal.Salt;

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["Ok"] = "Usuario actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Usuarios.AnyAsync(e => e.IdUsuario == id)) return NotFound();
                throw;
            }
        }

        // GET: UsuariosController/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdUsuario == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: UsuariosController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Usuario eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Método para enviar correos
        private void EnviarCorreo(string destino, string asunto, string mensajeHtml)
        {
            var section = _config.GetSection("Smtp");

            using (var smtp = new SmtpClient(section["Host"], int.Parse(section["Port"])))
            {
                smtp.Credentials = new NetworkCredential(section["User"], section["Pass"]);
                smtp.EnableSsl = bool.Parse(section["EnableSsl"]);
                smtp.UseDefaultCredentials = false;

                var mail = new MailMessage
                {
                    From = new MailAddress(section["User"], "Soporte TECA"),
                    Subject = asunto,
                    Body = mensajeHtml,
                    IsBodyHtml = true
                };

                mail.To.Add(destino);
                smtp.Send(mail);
            }
        }
    }
}
