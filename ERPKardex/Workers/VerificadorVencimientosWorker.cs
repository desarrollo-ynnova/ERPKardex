using ERPKardex.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ERPKardex.Workers
{
    public class VerificadorVencimientosWorker : BackgroundService
    {
        // Necesitamos el ScopeFactory para poder "crear" una instancia de la BD cuando queramos
        private readonly IServiceScopeFactory _scopeFactory;

        public VerificadorVencimientosWorker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Este bucle corre infinitamente mientras la app esté encendida
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. CREAR EL ÁMBITO (SCOPE) PARA USAR LA BASE DE DATOS
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // --- LÓGICA DE ACTUALIZACIÓN (La misma que tenías) ---

                        // A. Obtener IDs necesarios
                        var estAprobado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");
                        var estPagadoTotal = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Pagado Total" && e.Tabla == "FINANZAS");
                        var estVencido = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Vencido" && e.Tabla == "FINANZAS");

                        int idAprobado = estAprobado?.Id ?? 0;
                        int idPagadoTotal = estPagadoTotal?.Id ?? 0;
                        int idVencido = estVencido?.Id ?? 0;

                        // B. Buscar Órdenes Aprobadas que NO están pagadas NI vencidas
                        var compras = await _context.OrdenCompras
                            .Where(x => x.EstadoId == idAprobado && (x.EstadoPagoId == null || (x.EstadoPagoId != idPagadoTotal && x.EstadoPagoId != idVencido)))
                            .ToListAsync();

                        var servicios = await _context.OrdenServicios
                            .Where(x => x.EstadoId == idAprobado && (x.EstadoPagoId == null || (x.EstadoPagoId != idPagadoTotal && x.EstadoPagoId != idVencido)))
                            .ToListAsync();

                        bool huboCambios = false;
                        DateTime hoy = DateTime.Now.Date;

                        // C. Procesar Compras
                        foreach (var item in compras)
                        {
                            int dias = 0;
                            if (!string.IsNullOrEmpty(item.CondicionPago))
                            {
                                var match = Regex.Match(item.CondicionPago, @"\d+");
                                if (match.Success) int.TryParse(match.Value, out dias);
                            }

                            // Si la fecha límite ya pasó (ayer o antes), se marca VENCIDO
                            if ((item.FechaEmision?.AddDays(dias) ?? hoy) < hoy)
                            {
                                item.EstadoPagoId = idVencido;
                                _context.OrdenCompras.Update(item);
                                huboCambios = true;
                            }
                        }

                        // D. Procesar Servicios
                        foreach (var item in servicios)
                        {
                            int dias = 0;
                            if (!string.IsNullOrEmpty(item.CondicionPago))
                            {
                                var match = Regex.Match(item.CondicionPago, @"\d+");
                                if (match.Success) int.TryParse(match.Value, out dias);
                            }

                            if ((item.FechaEmision?.AddDays(dias) ?? hoy) < hoy)
                            {
                                item.EstadoPagoId = idVencido;
                                _context.OrdenServicios.Update(item);
                                huboCambios = true;
                            }
                        }

                        // E. Guardar todo
                        if (huboCambios)
                        {
                            await _context.SaveChangesAsync();
                            // Opcional: Loguear en consola para saber que funcionó
                            Console.WriteLine($"[AUTO-WORKER] Se actualizaron órdenes vencidas a las {DateTime.Now}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Si falla, lo escribimos en consola pero NO detenemos el worker
                    Console.WriteLine($"[AUTO-WORKER ERROR] {ex.Message}");
                }

                // 2. DORMIR EL ROBOT (ESPERAR)
                // Aquí defines cada cuánto tiempo revisa. 
                // Ejemplo: TimeSpan.FromHours(1) -> Cada 1 hora
                // Ejemplo: TimeSpan.FromMinutes(30) -> Cada 30 min
                await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
            }
        }
    }
}