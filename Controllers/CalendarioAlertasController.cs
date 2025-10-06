using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // 1. Para leer la configuración SMTP
using System.Net.Mail;      // 1. Para SmtpClient y MailMessage
using System.Net;           // 1. Para NetworkCredential

namespace SWebEnergia.Controllers
{
    public class CalendarioAlertasController : Controller
    {
        private readonly EnergiaContext _context;
        private readonly ILogger<CalendarioAlertasController> _logger;
        private readonly IConfiguration _configuration; // 2. Variable para la configuración

        // 2. Constructor modificado para inyectar IConfiguration
        public CalendarioAlertasController(EnergiaContext context, ILogger<CalendarioAlertasController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration; // Asignamos IConfiguration
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Cargando la vista del calendario.");
            await GenerarAlertasProximosMantenimientos();
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerEventos()
        {
            _logger.LogInformation("Iniciando la obtención de eventos para el calendario.");
            var todosLosEventos = new List<object>();

            // 1. Obtener eventos de Mantenimiento
            _logger.LogInformation("Consultando mantenimientos.");
            var mantenimientos = await _context.Mantenimientos
                .Include(m => m.SistemaRenovable)
                .ThenInclude(s => s.IdClienteNavigation)
                .Where(m => m.FechaProgramada.HasValue)
                .Select(m => new
                {
                    id = "m-" + m.IdMantenimiento,
                    title = $"{m.SistemaRenovable.IdClienteNavigation.Nombre} - Mantenimiento",
                    start = m.FechaProgramada.Value.ToString("yyyy-MM-dd"),
                    color = "#007bff", // Azul para mantenimientos
                    nombreCliente = m.SistemaRenovable.IdClienteNavigation.Nombre
                })
                .ToListAsync();

            _logger.LogInformation($"Se encontraron {mantenimientos.Count} mantenimientos.");
            todosLosEventos.AddRange(mantenimientos);

            // 2. Obtener eventos de Alertas
            _logger.LogInformation("Consultando alertas.");
            var alertas = await _context.Alertas
                .Where(a => a.Estado == "Activa")
                .ToListAsync();

            foreach (var alerta in alertas)
            {
                // Conexión entre Alertas y Mantenimientos
                var mantenimientoAsociado = await _context.Mantenimientos
                    .FirstOrDefaultAsync(m => m.IdSistema == alerta.IdSistema && m.FechaProgramada.HasValue);

                if (mantenimientoAsociado != null)
                {
                    todosLosEventos.Add(new
                    {
                        id = "a-" + alerta.IdAlerta,
                        title = $"ALERTA: {alerta.Mensaje}",
                        start = mantenimientoAsociado.FechaProgramada.Value.ToString("yyyy-MM-dd"),
                        color = "#dc3545" // Rojo para alertas
                    });
                }
            }

            _logger.LogInformation($"Se encontraron {alertas.Count} alertas.");
            return Json(todosLosEventos);
        }

        [HttpPost]
        public async Task<IActionResult> CrearMantenimiento([FromBody] Mantenimiento nuevoMantenimiento)
        {
            if (nuevoMantenimiento == null || !nuevoMantenimiento.FechaProgramada.HasValue)
            {
                return BadRequest(new { mensaje = "Datos del mantenimiento incompletos." });
            }

            var fechaProgramada = nuevoMantenimiento.FechaProgramada.Value.Date;

            var mantenimientosEnMismaFecha = await _context.Mantenimientos
                .CountAsync(m => m.FechaProgramada.HasValue && m.FechaProgramada.Value.Date == fechaProgramada);

            if (mantenimientosEnMismaFecha >= 2)
            {
                return StatusCode(403, new { mensaje = "Revisar calendario. Ya hay 2 mantenimientos programados para esta fecha." });
            }

            try
            {
                _context.Mantenimientos.Add(nuevoMantenimiento);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Mantenimiento guardado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el mantenimiento.");
                return StatusCode(500, new { mensaje = "Error interno del servidor al guardar el mantenimiento." });
            }
        }

        private async Task GenerarAlertasProximosMantenimientos()
        {
            _logger.LogInformation("Iniciando la generación de alertas para mantenimientos y vencimiento de productos.");
            var fechaActual = DateTime.Now.Date;

            // Rango 1: Alerta para mantenimientos YA programados (próximos 7 días)
            var fechaFinAlertaProgramada = fechaActual.AddDays(7);

            // --------------------------------------------------------------------------------
            // LÓGICA 1: ALERTA DE MANTENIMIENTO PROGRAMADO (0 a 7 días) - MODIFICADA
            // --------------------------------------------------------------------------------

            var proximosMantenimientosAlerta = await _context.Mantenimientos
         .Where(m => m.FechaProgramada.HasValue &&
                     m.FechaProgramada.Value.Date >= fechaActual &&
                     m.FechaProgramada.Value.Date < fechaFinAlertaProgramada)
         .Include(m => m.SistemaRenovable) // Incluye el sistema
         .ThenInclude(s => s.IdClienteNavigation) // Incluye el cliente del sistema
         .ToListAsync();

            foreach (var mantenimiento in proximosMantenimientosAlerta)
            {
                var cliente = mantenimiento.SistemaRenovable.IdClienteNavigation;
                var sistema = mantenimiento.SistemaRenovable; // 🛑 Acceso directo al sistema

                // Usamos la descripción del sistema en el mensaje de la Alerta DB
                var descripcionSistema = sistema.Descripcion ?? $"Sistema ID: {mantenimiento.IdSistema}";

                var mensaje = $"Alerta: Mantenimiento programado para el sistema de {cliente.Nombre} ({descripcionSistema}) el {mantenimiento.FechaProgramada.Value.ToShortDateString()}.";

                var alertaExistente = await _context.Alertas.FirstOrDefaultAsync(a => a.IdSistema == mantenimiento.IdSistema && a.Mensaje == mensaje && a.Estado == "Activa");

                if (alertaExistente == null)
                {
                    // ... (Código para crear la Alerta en DB se mantiene igual) ...

                    // Enviar la notificación por correo electrónico
                    if (!string.IsNullOrEmpty(cliente.Email))
                    {
                        var asunto = $"📅 Recordatorio: Mantenimiento Programado para el {mantenimiento.FechaProgramada.Value.ToShortDateString()}";

                        // 🛑 CUERPO DEL CORREO ACTUALIZADO CON MÁS DETALLES 🛑
                        var cuerpo = $@"
                        Estimado/a **{cliente.Nombre}**,

                        Le recordamos que tenemos un **Mantenimiento Programado** para su Sistema Renovable.

                        <br>
                        **Detalles del Sistema:**
                        <br>
                        * **Sistema:** **{descripcionSistema}**
                        * **Capacidad (kW):** {sistema.CapacidadKw?.ToString("0.##") ?? "N/D"}
                        * **Ubicación:** {sistema.Departamento} / {sistema.Provincia}
                        * **Dirección Exacta:** {sistema.DireccionExacta}

                        <br>
                        **Detalles del Servicio:**
                        <br>
                        * **Fecha Programada:** **{mantenimiento.FechaProgramada.Value.ToShortDateString()}**
                        * **Tipo de Mantenimiento:** {mantenimiento.TipoMantenimiento}

                        <br>
                        Por favor, asegúrese de que nuestro equipo técnico tenga acceso al sistema en la fecha indicada.
                        Si necesita reprogramar, contáctenos lo antes posible.

                        <br>
                        Saludos cordiales,
                        <br>
                        Equipo de Tecnoelectrica Industrial Andino SAC.";

                        await EnviarCorreoElectronico(cliente.Email, asunto, cuerpo);
                        _logger.LogInformation($"Correo de ALERTA (0-7 días) enviado a: {cliente.Email}");
                    }
                }
            }

            // --------------------------------------------------------------------------------
            // LÓGICA 2: ALERTA DE VENCIMIENTO DE COMPONENTES (basado en TipoComponentes.TiempoVida)
            // --------------------------------------------------------------------------------

            _logger.LogInformation("Iniciando verificación de vida útil de componentes instalados.");

            var diasAlertaVencimientoComponente = 90;
            var fechaLimiteAlerta = fechaActual.AddDays(diasAlertaVencimientoComponente);

            // Obtenemos todos los componentes instalados en sistemas con su tipo
            var componentesConSistema = await _context.Componentes
                .Include(c => c.TipoComponente)
                .Include(c => c.Sistema)
                    .ThenInclude(s => s.IdClienteNavigation)
                .Where(c => c.FechaInstalacion.HasValue && c.TipoComponente != null)
                .ToListAsync();

            foreach (var componente in componentesConSistema)
            {
                var vidaUtilAnios = componente.TipoComponente.TiempoVida;
                var fechaInstalacion = componente.FechaInstalacion.Value.Date;
                var fechaVencimiento = fechaInstalacion.AddYears(vidaUtilAnios);

                if (fechaVencimiento >= fechaActual && fechaVencimiento < fechaLimiteAlerta)
                {
                    var cliente = componente.Sistema?.IdClienteNavigation;
                    var sistema = componente.Sistema;
                    var tipoComponente = componente.TipoComponente;

                    if (cliente == null || sistema == null)
                        continue;

                    // Mensaje para la BD
                    var mensajeAlertaDB = $"¡ATENCIÓN! El componente '{tipoComponente.Descripcion}' (instalado en el sistema ID: {sistema.IdSistema} el {fechaInstalacion.ToShortDateString()}) tiene una vida útil que vence el **{fechaVencimiento.ToShortDateString()}** ({vidaUtilAnios} años). Comuníquese con Tecnoelectrica Industrial Andino S.A.C.";

                    // Asunto y cuerpo del correo
                    var asuntoCorreo = $"🔔 Acción Requerida: El componente '{tipoComponente.Descripcion}' está cerca de vencer";
                    var cuerpoCorreo = $@"
                    Estimado/a **{cliente.Nombre}**,

                    Le informamos que uno de los componentes instalados en su sistema renovable está próximo a vencer:

                    <br>
                    **Detalles del Componente:**
                    <br>
                    * **Componente:** **{tipoComponente.Descripcion}**
                    * **Sistema Asociado:** ID {sistema.IdSistema}
                    * **Fecha de Instalación:** {fechaInstalacion.ToShortDateString()}
                    * **Vida Útil:** {vidaUtilAnios} años
                    * **Fecha Estimada de Vencimiento:** **{fechaVencimiento.ToShortDateString()}**

                    <br>
                    Para evitar fallas o pérdidas de eficiencia, le recomendamos agendar una revisión o reemplazo del componente.
                    <br>
                    **Contáctenos para programar una inspección o cotización.**

                    <br>
                    Saludos cordiales,
                    <br>
                    Equipo de Tecnoelectrica Industrial Andino SAC.";

                    // Verificar si ya existe una alerta
                    var alertaExistente = await _context.Alertas
                        .FirstOrDefaultAsync(a =>
                            a.IdSistema == sistema.IdSistema &&
                            a.Tipo == "Vencimiento Componente" &&
                            a.Mensaje == mensajeAlertaDB &&
                            a.Estado == "Activa");

                    if (alertaExistente == null)
                    {
                        var nuevaAlerta = new Alerta
                        {
                            IdSistema = sistema.IdSistema,
                            Tipo = "Vencimiento Componente",
                            Mensaje = mensajeAlertaDB,
                            Estado = "Activa",
                            FechaGeneracion = DateTime.Now
                        };

                        _context.Alertas.Add(nuevaAlerta);
                        _logger.LogInformation($"✅ Alerta generada para componente: '{tipoComponente.Descripcion}' en sistema ID: {sistema.IdSistema}");

                        if (!string.IsNullOrEmpty(cliente.Email))
                        {
                            await EnviarCorreoElectronico(cliente.Email, asuntoCorreo, cuerpoCorreo);
                            _logger.LogInformation($"📧 Correo de vencimiento enviado a: {cliente.Email}");
                        }
                    }
                }
            }

            // --------------------------------------------------------------------------------
            // LÓGICA 3: ALERTA DE MANTENIMIENTO PERIÓDICO DE COMPONENTES
            // --------------------------------------------------------------------------------

            _logger.LogInformation("Verificando frecuencia de mantenimiento de componentes instalados...");

            // Obtenemos nuevamente los componentes instalados con su sistema y tipo
            var componentesMantenimiento = await _context.Componentes
                .Include(c => c.TipoComponente)
                .Include(c => c.Sistema)
                    .ThenInclude(s => s.IdClienteNavigation)
                .Where(c => c.FechaInstalacion.HasValue && c.TipoComponente != null && c.Sistema != null)
                .ToListAsync();

            foreach (var componente in componentesMantenimiento)
            {
                var frecuenciaMes = componente.TipoComponente.FrecuenciaMantenimientoMes;
                if (!frecuenciaMes.HasValue || frecuenciaMes.Value <= 0)
                    continue; // No requiere mantenimiento periódico

                var cliente = componente.Sistema.IdClienteNavigation;
                var sistema = componente.Sistema;
                var tipoComponente = componente.TipoComponente;

                var fechaBase = componente.UltimoFechaMantenimiento ?? componente.FechaInstalacion;
                if (!fechaBase.HasValue)
                    continue;

                var fechaProximoMantenimiento = fechaBase.Value.AddMonths((int)frecuenciaMes.Value);

                if (fechaProximoMantenimiento.Date >= fechaActual && fechaProximoMantenimiento.Date <= fechaActual.AddDays(15)) // Alerta si falta <=15 días
                {
                    var mensajeAlertaDB = $"Recordatorio: El componente '{tipoComponente.Descripcion}' instalado en el sistema ID {sistema.IdSistema} requiere mantenimiento según su frecuencia ({frecuenciaMes.Value / 12:0.#} veces por año). Fecha estimada: {fechaProximoMantenimiento:dd/MM/yyyy}.";

                    // Evitar duplicados
                    var alertaExistente = await _context.Alertas.FirstOrDefaultAsync(a =>
                        a.IdSistema == sistema.IdSistema &&
                        a.Tipo == "Mantenimiento Componente" &&
                        a.Mensaje == mensajeAlertaDB &&
                        a.Estado == "Activa");

                    if (alertaExistente == null)
                    {
                        var nuevaAlerta = new Alerta
                        {
                            IdSistema = sistema.IdSistema,
                            Tipo = "Mantenimiento Componente",
                            Mensaje = mensajeAlertaDB,
                            Estado = "Activa",
                            FechaGeneracion = DateTime.Now
                        };

                        _context.Alertas.Add(nuevaAlerta);
                        _logger.LogInformation($"🔧 Alerta generada para mantenimiento de componente: '{tipoComponente.Descripcion}' (sistema ID: {sistema.IdSistema})");

                        if (!string.IsNullOrEmpty(cliente?.Email))
                        {
                            var asuntoCorreo = $"🔧 Mantenimiento requerido para el componente '{tipoComponente.Descripcion}'";
                            var cuerpoCorreo = $@"
                            Estimado/a **{cliente.Nombre}**,

                            De acuerdo con el cronograma de mantenimiento, su componente **{tipoComponente.Descripcion}** instalado en el sistema ID **{sistema.IdSistema}** requiere mantenimiento técnico preventivo.

                            <br>
                            **Detalles del Componente:**
                            <br>
                            * **Fecha de última revisión o instalación:** {fechaBase.Value.ToShortDateString()}
                            * **Frecuencia de mantenimiento:** cada {frecuenciaMes.Value / 12:0.#} año(s)
                            * **Próximo mantenimiento estimado:** **{fechaProximoMantenimiento.ToShortDateString()}**

                            <br>
                            Para garantizar un funcionamiento óptimo de su sistema, le recomendamos agendar el mantenimiento a la brevedad.

                            <br>
                            Saludos cordiales,
                            <br>
                            Equipo de Tecnoelectrica Industrial Andino SAC.";

                            await EnviarCorreoElectronico(cliente.Email, asuntoCorreo, cuerpoCorreo);
                            _logger.LogInformation($"📧 Correo de mantenimiento periódico enviado a: {cliente.Email}");
                        }
                    }
                }
            }



            // --------------------------------------------------------------------------------
            // LÓGICA 4: CORREO DE RECORDATORIO A 14 DÍAS 
            // --------------------------------------------------------------------------------

            var mantenimientosParaCorreo = await _context.Mantenimientos
                .Where(m => m.FechaProgramada.HasValue && m.FechaProgramada.Value.Date == fechaActual.AddDays(14).Date && m.TipoMantenimiento == "Preventivo")
                .Include(m => m.SistemaRenovable)
                .ThenInclude(s => s.IdClienteNavigation)
                .ToListAsync();

            foreach (var mantenimiento in mantenimientosParaCorreo)
            {
                var cliente = mantenimiento.SistemaRenovable.IdClienteNavigation;

                // Mejorar Asunto y Cuerpo del correo de recordatorio de 14 días
                var asunto = $"🗓️ Recordatorio: Su Mantenimiento Preventivo es en 14 días";
                var cuerpo = $@"
                Estimado/a **{cliente.Nombre}**,

                Le recordamos su cita para el Mantenimiento Preventivo de su sistema renovable.

                <br>
                * **Fecha Programada:** **{mantenimiento.FechaProgramada.Value.ToShortDateString()}**
                * **Anticipación:** 14 días

                <br>
                Por favor, confirme su disponibilidad o notifique cualquier necesidad de cambio de fecha.

                <br>
                Saludos,
                <br>
               Equipo de Tecnoelectrica Industrial Andino SAC.";

                if (!string.IsNullOrEmpty(cliente.Email))
                {
                    await EnviarCorreoElectronico(cliente.Email, asunto, cuerpo);
                }
            }

            // Guardar todos los cambios (nuevas alertas) en la base de datos
            await _context.SaveChangesAsync();
            _logger.LogInformation("Proceso de generación de alertas y correos finalizado.");
        }


        private async Task EnviarCorreoElectronico(string email, string asunto, string cuerpo)
        {
            _logger.LogInformation($"Iniciando envío de correo a {email} con el asunto: {asunto}");

            try
            {
                // Implementación real usando la configuración "Smtp" de appsettings.json
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var smtpUser = _configuration["Smtp:User"];
                var smtpPass = _configuration["Smtp:Pass"];
                var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    _logger.LogError("Fallo al enviar correo: Configuración SMTP incompleta o nula. Revise appsettings.json.");
                    return;
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                using (var message = new MailMessage())
                {
                    // Configuración del cliente SMTP
                    client.EnableSsl = enableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);

                    // Contenido del mensaje
                    message.From = new MailAddress(smtpUser!, "TECA - Alertas");
                    message.To.Add(new MailAddress(email));
                    message.Subject = asunto;
                    message.Body = $"<html><body>{cuerpo}</body></html>";
                    message.IsBodyHtml = true;

                    // Envío asíncrono
                    await client.SendMailAsync(message);
                    _logger.LogInformation($"Correo enviado exitosamente a: {email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error CRÍTICO al enviar el correo a {email}. Verifique las credenciales (Contraseña de Aplicación) y la configuración de SMTP. Detalles: {ex.Message}");
            }
        }
    }
}