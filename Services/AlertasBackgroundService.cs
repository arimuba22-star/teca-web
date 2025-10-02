using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SWebEnergia.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SWebEnergia.Services
{
    public class AlertasBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AlertasBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<EnergiaContext>();

                    var hoy = DateTime.Today;

                    var sistemas = await context.SistemasRenovables
                        .Include(s => s.Componentes)
                        .ThenInclude(c => c.TipoComponente) // ✅ propiedad correcta
                        .ToListAsync();

                    foreach (var sistema in sistemas)
                    {
                        foreach (var componente in sistema.Componentes)
                        {
                            // Verificar si existe la frecuencia
                            if (componente.TipoComponente?.FrecuenciaMantenimientoMes == null)
                                continue;

                            // Convertimos a int para AddMonths()
                            int frecuenciaMeses = (int)Math.Round(componente.TipoComponente.FrecuenciaMantenimientoMes.Value);

                            // Usamos FechaInstalacion como respaldo si UltimoFechaMantenimiento es null
                            var ultima = componente.UltimoFechaMantenimiento ?? componente.FechaInstalacion;
                            if (ultima == null)
                                continue; // si tampoco hay instalación, no calculamos

                            var proxima = ultima.Value.AddMonths(frecuenciaMeses);

                            // Verificamos si el mantenimiento está dentro de los próximos 7 días
                            if ((proxima - hoy).TotalDays <= 7 && (proxima - hoy).TotalDays >= 0)
                            {
                                bool yaExiste = await context.Alertas
                                    .AnyAsync(a => a.IdSistema == sistema.IdSistema &&
                                                   a.Mensaje.Contains("preventivo") &&
                                                   a.Estado == "Pendiente");

                                if (!yaExiste)
                                {
                                    context.Alertas.Add(new Alerta
                                    {
                                        IdSistema = sistema.IdSistema,
                                        Tipo = "Preventivo",
                                        Mensaje = $"El mantenimiento preventivo vence el {proxima:dd/MM/yyyy}.",
                                        Estado = "Pendiente",
                                        FechaGeneracion = DateTime.Now
                                    });
                                }
                            }
                        }

                    }

                    await context.SaveChangesAsync();
                }

                // ⏳ Espera 24 horas antes de volver a ejecutar
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
