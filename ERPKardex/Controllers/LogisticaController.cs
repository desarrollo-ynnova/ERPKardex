using ERPKardex.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ERPKardex.Controllers
{
    public class LogisticaController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public LogisticaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetDashboardData(int? empresaId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;
                int idEmpresaFiltro = esGlobal ? (empresaId ?? 0) : miEmpresaId;

                var fInicio = fechaInicio ?? DateTime.Now.AddMonths(-6);
                var fFin = fechaFin ?? DateTime.Now;

                // =================================================================================
                // 1. DATA BASE (FILTRADA)
                // =================================================================================

                // A. Órdenes
                var qOC = _context.OrdenCompras.AsQueryable();
                var qOS = _context.OrdenServicios.AsQueryable();

                if (idEmpresaFiltro > 0)
                {
                    qOC = qOC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qOS = qOS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }
                qOC = qOC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qOS = qOS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);

                // B. Requerimientos
                var qRC = _context.ReqCompras.AsQueryable();
                var qRS = _context.ReqServicios.AsQueryable();

                if (idEmpresaFiltro > 0)
                {
                    qRC = qRC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRS = qRS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }
                qRC = qRC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRS = qRS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);

                // Helper para nombres de estados (OPERATIVOS y FINANCIEROS)
                var estadosDb = await _context.Estados.ToListAsync();
                var estadosFinanzas = estadosDb.Where(e => e.Tabla == "FINANZAS").ToList();

                // =================================================================================
                // 2. TORTAS OPERATIVAS (Por Estado del Documento: Aprobado, Anulado...)
                // =================================================================================

                // Req Compras
                var kpiRC = await qRC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
                var chartRC = kpiRC.Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                // Req Servicios
                var kpiRS = await qRS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
                var chartRS = kpiRS.Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                // Orden Compras (OPERATIVO)
                var kpiOC = await qOC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
                var chartOC = kpiOC.Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                // Orden Servicios (OPERATIVO)
                var kpiOS = await qOS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
                var chartOS = kpiOS.Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();


                // =================================================================================
                // 3. TORTAS FINANCIERAS (Por Estado de Pago: Vencido, Pagado...)
                // =================================================================================
                // AHORA ES DIRECTO A LA BD (GROUP BY EstadoPagoId)

                // Finanzas Compras
                var kpiPagoOC = await qOC
                    .GroupBy(x => x.EstadoPagoId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToListAsync();

                var chartPagoOC = kpiPagoOC.Select(x => new
                {
                    name = x.Id == null ? "Pendiente Pago" : (estadosFinanzas.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Otro"),
                    value = x.Count
                }).ToList();

                // Finanzas Servicios
                var kpiPagoOS = await qOS
                    .GroupBy(x => x.EstadoPagoId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToListAsync();

                var chartPagoOS = kpiPagoOS.Select(x => new
                {
                    name = x.Id == null ? "Pendiente Pago" : (estadosFinanzas.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Otro"),
                    value = x.Count
                }).ToList();

                // =================================================================================
                // 4. EVOLUCIÓN MENSUAL
                // =================================================================================
                var dataOC_List = await qOC.Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();
                var dataOS_List = await qOS.Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();

                var fechasUnicas = dataOC_List.Select(x => new { x.FechaEmision.Value.Year, x.FechaEmision.Value.Month })
                    .Union(dataOS_List.Select(x => new { x.FechaEmision.Value.Year, x.FechaEmision.Value.Month }))
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

                var categoriasMes = new List<string>();
                var serieCompra = new List<decimal>();
                var serieServicio = new List<decimal>();

                foreach (var f in fechasUnicas)
                {
                    categoriasMes.Add(new DateTime(f.Year, f.Month, 1).ToString("MMM-yyyy", CultureInfo.CreateSpecificCulture("es-PE")));

                    decimal sumOC = dataOC_List.Where(x => x.FechaEmision.Value.Year == f.Year && x.FechaEmision.Value.Month == f.Month)
                        .Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);
                    serieCompra.Add(Math.Round(sumOC, 2));

                    decimal sumOS = dataOS_List.Where(x => x.FechaEmision.Value.Year == f.Year && x.FechaEmision.Value.Month == f.Month)
                        .Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);
                    serieServicio.Add(Math.Round(sumOS, 2));
                }

                // =================================================================================
                // 5. KPIS Y RETORNO
                // =================================================================================
                decimal totalSoles = serieCompra.Sum() + serieServicio.Sum();
                var idsOC = await qOC.Select(x => x.Id).ToListAsync();
                var idsOS = await qOS.Select(x => x.Id).ToListAsync();
                var montoPagado = await _context.OrdenPagos
                    .Where(p => (p.OrdenCompraId.HasValue && idsOC.Contains(p.OrdenCompraId.Value)) ||
                                (p.OrdenServicioId.HasValue && idsOS.Contains(p.OrdenServicioId.Value)))
                    .SumAsync(p => p.MontoAbonado);

                return Json(new
                {
                    status = true,
                    kpis = new
                    {
                        montoTotal = totalSoles,
                        countOC = await qOC.CountAsync() + await qOS.CountAsync(),
                        countReq = await qRC.CountAsync() + await qRS.CountAsync(),
                        montoPagado = montoPagado
                    },
                    charts = new
                    {
                        evolucion = new { categories = categoriasMes, compras = serieCompra, servicios = serieServicio },
                        // OPERATIVO
                        pieRC = chartRC,
                        pieRS = chartRS,
                        pieOC = chartOC,
                        pieOS = chartOS,
                        // FINANCIERO (VENCIDO / PAGADO / PARCIAL)
                        piePagoOC = chartPagoOC,
                        piePagoOS = chartPagoOS
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
    }
}