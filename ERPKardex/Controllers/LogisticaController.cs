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
                // 1. DATA BASE (ÓRDENES Y REQUERIMIENTOS)
                // =================================================================================

                var qOC = _context.OrdenCompras.AsQueryable();
                var qOS = _context.OrdenServicios.AsQueryable();
                var qRC = _context.ReqCompras.AsQueryable();
                var qRS = _context.ReqServicios.AsQueryable();

                if (idEmpresaFiltro > 0)
                {
                    qOC = qOC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qOS = qOS.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRC = qRC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRS = qRS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }

                // Filtro Fechas
                qOC = qOC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qOS = qOS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRC = qRC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRS = qRS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);

                // Helper Estados
                var estadosDb = await _context.Estados.ToListAsync();
                var estadosFinanzas = estadosDb.Where(e => e.Tabla == "FINANZAS").ToList();

                // =================================================================================
                // 2. DATA PAGOS (CORRECCIÓN: JOINS MANUALES)
                // =================================================================================
                // Necesitamos filtrar los pagos por la empresa de la orden asociada

                // A. Pagos de Compras
                var qPagosC = from p in _context.OrdenPagos
                              join o in _context.OrdenCompras on p.OrdenCompraId equals o.Id
                              where p.EstadoId == 1 // Pagos activos
                                 && p.FechaPago >= fInicio && p.FechaPago <= fFin
                              select new { p, EmpresaId = o.EmpresaId };

                // B. Pagos de Servicios
                var qPagosS = from p in _context.OrdenPagos
                              join o in _context.OrdenServicios on p.OrdenServicioId equals o.Id
                              where p.EstadoId == 1
                                 && p.FechaPago >= fInicio && p.FechaPago <= fFin
                              select new { p, EmpresaId = o.EmpresaId };

                // Aplicar filtro empresa a los pagos
                if (idEmpresaFiltro > 0)
                {
                    qPagosC = qPagosC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qPagosS = qPagosS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }

                // =================================================================================
                // 3. TORTAS Y KPIS (LÓGICA CONSOLIDADA)
                // =================================================================================

                // --- Tortas Operativas ---
                var chartRC = (await qRC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                var chartRS = (await qRS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                var chartOC = (await qOC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                var chartOS = (await qOS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                // --- Tortas Financieras ---
                var chartPagoOC = (await qOC.GroupBy(x => x.EstadoPagoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                  .Select(x => new { name = x.Id == null ? "Pendiente Pago" : (estadosFinanzas.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Otro"), value = x.Count }).ToList();

                var chartPagoOS = (await qOS.GroupBy(x => x.EstadoPagoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                  .Select(x => new { name = x.Id == null ? "Pendiente Pago" : (estadosFinanzas.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Otro"), value = x.Count }).ToList();


                // =================================================================================
                // 4. DATA PARA EVOLUCIÓN (LISTAS EN MEMORIA)
                // =================================================================================

                // Ejecutado (Órdenes)
                var dataOC = await qOC.Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();
                var dataOS = await qOS.Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();

                // Pagado (Pagos - Join ya resuelto)
                var dataPagosC = await qPagosC.Select(x => new { x.p.FechaPago, Monto = x.p.MontoAbonado, Moneda = x.p.MonedaOrdenId, TC = x.p.TipoCambioPago }).ToListAsync();
                var dataPagosS = await qPagosS.Select(x => new { x.p.FechaPago, Monto = x.p.MontoAbonado, Moneda = x.p.MonedaOrdenId, TC = x.p.TipoCambioPago }).ToListAsync();

                // Unificar Fechas (Meses)
                var fechasUnicas = dataOC.Select(x => new { x.FechaEmision.Value.Year, x.FechaEmision.Value.Month })
                    .Union(dataOS.Select(x => new { x.FechaEmision.Value.Year, x.FechaEmision.Value.Month }))
                    .Union(dataPagosC.Select(x => new { x.FechaPago.Year, x.FechaPago.Month }))
                    .Union(dataPagosS.Select(x => new { x.FechaPago.Year, x.FechaPago.Month }))
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                var categoriasMes = new List<string>();

                // Series Compras
                var serieCompraEjec = new List<decimal>();
                var serieCompraPag = new List<decimal>();

                // Series Servicios
                var serieServEjec = new List<decimal>();
                var serieServPag = new List<decimal>();

                foreach (var f in fechasUnicas)
                {
                    categoriasMes.Add(new DateTime(f.Year, f.Month, 1).ToString("MMM-yyyy", CultureInfo.CreateSpecificCulture("es-PE")));

                    // 1. COMPRAS
                    decimal sumOC_Ejec = dataOC.Where(x => x.FechaEmision.Value.Year == f.Year && x.FechaEmision.Value.Month == f.Month)
                        .Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);

                    decimal sumOC_Pag = dataPagosC.Where(x => x.FechaPago.Year == f.Year && x.FechaPago.Month == f.Month)
                        .Sum(x => x.Moneda == 2 ? x.Monto * x.TC : x.Monto).GetValueOrDefault();

                    serieCompraEjec.Add(Math.Round(sumOC_Ejec, 2));
                    serieCompraPag.Add(Math.Round(sumOC_Pag, 2));

                    // 2. SERVICIOS
                    decimal sumOS_Ejec = dataOS.Where(x => x.FechaEmision.Value.Year == f.Year && x.FechaEmision.Value.Month == f.Month)
                        .Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);

                    decimal sumOS_Pag = dataPagosS.Where(x => x.FechaPago.Year == f.Year && x.FechaPago.Month == f.Month)
                        .Sum(x => x.Moneda == 2 ? x.Monto * x.TC : x.Monto).GetValueOrDefault();

                    serieServEjec.Add(Math.Round(sumOS_Ejec, 2));
                    serieServPag.Add(Math.Round(sumOS_Pag, 2));
                }

                // =================================================================================
                // 5. RETORNO JSON
                // =================================================================================
                decimal totalEjecutado = serieCompraEjec.Sum() + serieServEjec.Sum();
                decimal totalPagado = serieCompraPag.Sum() + serieServPag.Sum();

                return Json(new
                {
                    status = true,
                    kpis = new
                    {
                        montoTotal = totalEjecutado,
                        countOC = await qOC.CountAsync() + await qOS.CountAsync(),
                        countReq = await qRC.CountAsync() + await qRS.CountAsync(),
                        montoPagado = totalPagado
                    },
                    charts = new
                    {
                        evolucionCategories = categoriasMes,
                        // GRÁFICO 1: COMPRAS
                        seriesCompra = new { ejecutado = serieCompraEjec, pagado = serieCompraPag },
                        // GRÁFICO 2: SERVICIOS
                        seriesServicio = new { ejecutado = serieServEjec, pagado = serieServPag },

                        // TORTAS
                        pieRC = chartRC,
                        pieRS = chartRS,
                        pieOC = chartOC,
                        pieOS = chartOS,
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