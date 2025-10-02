using Microsoft.Extensions.Configuration;
using SWebEnergia.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Linq; // Necesario para .Any() en algunos entornos

namespace SWebEnergia.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var smtp = _configuration.GetSection("Smtp");

            var client = new SmtpClient(smtp["Host"])
            {
                Port = int.Parse(smtp["Port"]!),
                Credentials = new NetworkCredential(smtp["User"], smtp["Pass"]),
                EnableSsl = bool.Parse(smtp["EnableSsl"]!)
            };

            var mensaje = new MailMessage
            {
                From = new MailAddress(smtp["User"]!),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            mensaje.To.Add(destinatario);

            await client.SendMailAsync(mensaje);
        }

        // 🛑 MÉTODO ACTUALIZADO 🛑
        public async Task EnviarCorreoNotificacionAsync(string email, string nombreDestinatario, SistemasRenovable sistema, bool esDueno = false)
        {
            // Personaliza el asunto y el saludo
            string asunto = esDueno
                ? $"NOTIFICACIÓN: Nuevo Sistema Registrado - {sistema.Descripcion}"
                : "Registro de tu Sistema Renovable Completado";

            string saludo = esDueno ? "Estimado Administrador," : $"Hola {nombreDestinatario},";
            string parrafo = esDueno ?
                $"Se ha **registrado** un nuevo sistema para el cliente **{sistema.IdClienteNavigation?.Nombre ?? "N/D"}** en la plataforma. Los detalles son:" :
                "Se ha registrado un nuevo sistema renovable a tu nombre con la siguiente información:";

            // Tabla principal con datos del sistema
            string cuerpoHtml = $@"
                <h3>{saludo}</h3>
                <p>{parrafo}</p>

                <table style='border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; margin-bottom: 20px;'>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Cliente:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.IdClienteNavigation?.Nombre ?? "N/D"}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Tipo de Sistema:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.IdTipoSistemaNavigation?.Nombre ?? "N/D"}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Descripción:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.Descripcion}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Estado:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.Estado}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Fecha de Instalación:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.FechaInstalacion.ToString("dd/MM/yyyy")}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Capacidad (kW):</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.CapacidadKw?.ToString("0.##") ?? "N/D"}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Ubicación:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.Departamento} - {sistema.Provincia}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Dirección Exacta:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.DireccionExacta}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Referencia:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.Referencia}</td></tr>
                    <tr><td style='padding: 8px; border: 1px solid #ccc;'><strong>Observaciones:</strong></td><td style='padding: 8px; border: 1px solid #ccc;'>{sistema.Observaciones}</td></tr>
                </table>
            ";

            // Si hay componentes, agregarlos como tabla
            if (sistema.Componentes != null && sistema.Componentes.Any())
            {
                cuerpoHtml += @"
            <h4 style='font-family: Arial, sans-serif;'>Componentes del sistema</h4>
            <table style='border-collapse: collapse; width: 100%; font-family: Arial, sans-serif;'>
                <thead>
                    <tr style='background-color: #f2f2f2;'>
                        <th style='padding: 8px; border: 1px solid #ccc;'>Tipo</th>
                        <th style='padding: 8px; border: 1px solid #ccc;'>Descripción</th>
                        <th style='padding: 8px; border: 1px solid #ccc;'>Capacidad (kW)</th>
                        <th style='padding: 8px; border: 1px solid #ccc;'>Fecha Instalación</th>
                    </tr>
                </thead>
                <tbody>";

                foreach (var comp in sistema.Componentes)
                {
                    cuerpoHtml += $@"
                    <tr>
                        <td style='padding: 8px; border: 1px solid #ccc;'>{comp.TipoComponente?.Descripcion ?? "N/D"}</td>
                        <td style='padding: 8px; border: 1px solid #ccc;'>{comp.Descripcion}</td>
                        <td style='padding: 8px; border: 1px solid #ccc;'>{comp.CapacidadEnergetica?.ToString("0.##") ?? "N/D"}</td>
                        <td style='padding: 8px; border: 1px solid #ccc;'>{comp.FechaInstalacion?.ToString("dd/MM/yyyy") ?? "N/D"}</td>
                    </tr>";
                }

                cuerpoHtml += @"
                </tbody>
            </table>";
            }

            cuerpoHtml += "<p style='font-family: Arial, sans-serif;'>Gracias por confiar en nuestro servicio.</p>";

            await EnviarCorreoAsync(email, asunto, cuerpoHtml);
        }
    }
}