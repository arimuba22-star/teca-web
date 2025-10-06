using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using SWebEnergia.Security;
using System.Net;
using System.Net.Mail;

namespace SWebEnergia.Controllers
{
    public class AccountController : Controller
    {
        private readonly EnergiaContext _context;
        private readonly IConfiguration _config;

        public AccountController(EnergiaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Login
        public IActionResult Login() => View();

        // POST: Login
        // POST: Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Incluimos el rol para poder usar su nombre
            var usuario = _context.Usuarios
                .Include(u => u.IdRolNavigation) // 👈 necesario para obtener el nombre del rol
                .FirstOrDefault(u => (u.Email == email || u.Nombre == email) && u.Activo != false);

            if (usuario == null)
            {
                ViewBag.Error = "❌ Usuario no encontrado o inactivo.";
                return View();
            }

            if (!PasswordVerifier.Verify(password, usuario.Salt, usuario.PasswordHash, out _))
            {
                ViewBag.Error = "❌ Contraseña incorrecta.";
                return View();
            }

            // Guardamos en sesión
            HttpContext.Session.SetString("Usuario", usuario.Nombre);
            HttpContext.Session.SetInt32("IdUsuario", usuario.IdUsuario);
            HttpContext.Session.SetInt32("IdRol", usuario.IdRol);

            // 👇 NUEVO: guardamos también el nombre del rol (ej. "Administrador")
            if (usuario.IdRolNavigation != null)
                HttpContext.Session.SetString("Rol", usuario.IdRolNavigation.Nombre);

            return RedirectToAction("Index", "Home");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // 🔹 FORGOT PASSWORD
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                TempData["Error"] = "❌ No se encontró un usuario con ese correo.";
                return RedirectToAction("ForgotPassword");
            }

            var token = Guid.NewGuid().ToString();
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email }, Request.Scheme);

            string mensaje = $@"
        <p>Estimado/a <b>{usuario.Nombre}</b>,</p>
        <p>Recibimos una solicitud para restablecer su contraseña en <b>TECA</b>.</p>
        <p>Haga clic en el siguiente enlace para continuar:</p>
        <p><a href='{resetLink}' style='color:#5A1C16;font-weight:bold;'>🔑 Restablecer mi contraseña</a></p>
        <br/>
        <p>Si no solicitó este cambio, ignore este correo.</p>
    ";

            EnviarCorreo(email, "Recuperar contraseña - TECA", mensaje);

            TempData["Ok"] = "✅ Se ha enviado un enlace a su correo para restablecer la contraseña.";
            return RedirectToAction("ForgotPassword");
        }


        // 🔹 ResetPassword
        public IActionResult ResetPassword(string token, string email)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                ViewBag.Error = "❌ Usuario no encontrado.";
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "❌ Las contraseñas no coinciden.";
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            var salt = PasswordVerifier.GenerateSalt();
            var hash = PasswordVerifier.HashPassword(newPassword, salt);

            usuario.Salt = salt;
            usuario.PasswordHash = hash;
            _context.SaveChanges();

            HttpContext.Session.Clear();
            ViewBag.Success = "✅ Su contraseña ha sido restablecida correctamente.";
            return View();
        }


        // 🔹 ChangePassword (desde link enviado por admin)
        public IActionResult ChangePassword(string email, string token)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string email, string token, string newPassword, string confirmPassword)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                TempData["Error"] = "❌ Usuario no encontrado.";
                return RedirectToAction("ChangePassword", new { email, token });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "❌ Las contraseñas no coinciden.";
                return RedirectToAction("ChangePassword", new { email, token });
            }

            var salt = PasswordVerifier.GenerateSalt();
            var hash = PasswordVerifier.HashPassword(newPassword, salt);

            usuario.Salt = salt;
            usuario.PasswordHash = hash;
            _context.SaveChanges();

            HttpContext.Session.Clear();
            TempData["Ok"] = "✅ Contraseña cambiada correctamente. Ahora puede iniciar sesión.";
            return RedirectToAction("ChangePassword", new { email, token });
        }

        // 🔹 Método para enviar correos (solo Gmail configurado en appsettings.json)
        private void EnviarCorreo(string destino, string asunto, string mensajeHtml)
        {
            var section = _config.GetSection("Smtp");

            using var smtp = new SmtpClient(section["Host"], int.Parse(section["Port"]));
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
