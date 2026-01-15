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
        public async Task<JsonResult> GetDashboardData(int? empresaId, int? monedaId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;
                int idEmpresaFiltro = esGlobal ? (empresaId ?? 0) : miEmpresaId;

                var fInicio = fechaInicio ?? DateTime.Now.AddMonths(-6);
                var fFin = fechaFin ?? DateTime.Now;

                // Definir moneda de visualización (Por defecto 1: Soles)
                // 1: SOLES, 2: DÓLARES
                int monedaVisualizacion = (monedaId.HasValue && monedaId.Value == 2) ? 2 : 1;

                // =================================================================================
                // 1. CONSULTAS BASE (SIN FILTRO DE MONEDA)
                // =================================================================================
                // Traemos TODO porque necesitamos convertir, no ocultar.

                var qOC = _context.OrdenCompras.AsQueryable();
                var qOS = _context.OrdenServicios.AsQueryable();
                var qRC = _context.ReqCompras.AsQueryable();
                var qRS = _context.ReqServicios.AsQueryable();

                // Filtro Empresa
                if (idEmpresaFiltro > 0)
                {
                    qOC = qOC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qOS = qOS.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRC = qRC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qRS = qRS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }

                // Filtro Fecha
                qOC = qOC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qOS = qOS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRC = qRC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qRS = qRS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);

                // Helper Estados
                var estadosDb = await _context.Estados.ToListAsync();
                var estadosFinanzas = estadosDb.Where(e => e.Tabla == "FINANZAS").ToList();

                // =================================================================================
                // 2. DATA PAGOS (Join Manual)
                // =================================================================================
                var qPagosC = from p in _context.OrdenPagos
                              join o in _context.OrdenCompras on p.OrdenCompraId equals o.Id
                              where p.EstadoId == 1 && p.FechaPago >= fInicio && p.FechaPago <= fFin
                              select new { p, EmpresaId = o.EmpresaId, MonedaOrden = o.MonedaId };

                var qPagosS = from p in _context.OrdenPagos
                              join o in _context.OrdenServicios on p.OrdenServicioId equals o.Id
                              where p.EstadoId == 1 && p.FechaPago >= fInicio && p.FechaPago <= fFin
                              select new { p, EmpresaId = o.EmpresaId, MonedaOrden = o.MonedaId };

                if (idEmpresaFiltro > 0)
                {
                    qPagosC = qPagosC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qPagosS = qPagosS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }

                // =================================================================================
                // 3. TORTAS (KPIS)
                // =================================================================================
                var chartRC = (await qRC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();
                var chartRS = (await qRS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();
                var chartOC = (await qOC.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();
                var chartOS = (await qOS.GroupBy(x => x.EstadoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                              .Select(x => new { name = estadosDb.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Desconocido", value = x.Count }).ToList();

                var chartPagoOC = (await qOC.GroupBy(x => x.EstadoPagoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                  .Select(x => new { name = x.Id == null ? "Pendiente Pago" : (estadosFinanzas.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Otro"), value = x.Count }).ToList();
                var chartPagoOS = (await qOS.GroupBy(x => x.EstadoPagoId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync())
                                  .Select(x => new { name = x.Id == null ? "Pendiente Pago" : (estadosFinanzas.FirstOrDefault(e => e.Id == x.Id)?.Nombre ?? "Otro"), value = x.Count }).ToList();

                // =================================================================================
                // 4. EVOLUCIÓN MENSUAL (LÓGICA DE CONVERSIÓN DE MONEDA)
                // =================================================================================

                // Traemos la data cruda (Total + Moneda Original + TC Original)
                var dataOC = await qOC.Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();
                var dataOS = await qOS.Select(x => new { x.FechaEmision, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1 }).ToListAsync();

                // En pagos, el TC importante es el del PAGO (TipoCambioPago), no el de la orden
                var dataPagosC = await qPagosC.Select(x => new { x.p.FechaPago, Monto = x.p.MontoAbonado, Moneda = x.p.MonedaOrdenId, TC = x.p.TipoCambioPago }).ToListAsync();
                var dataPagosS = await qPagosS.Select(x => new { x.p.FechaPago, Monto = x.p.MontoAbonado, Moneda = x.p.MonedaOrdenId, TC = x.p.TipoCambioPago }).ToListAsync();

                var fechasUnicas = dataOC.Select(x => new { x.FechaEmision.Value.Year, x.FechaEmision.Value.Month })
                    .Union(dataOS.Select(x => new { x.FechaEmision.Value.Year, x.FechaEmision.Value.Month }))
                    .Union(dataPagosC.Select(x => new { x.FechaPago.Year, x.FechaPago.Month }))
                    .Union(dataPagosS.Select(x => new { x.FechaPago.Year, x.FechaPago.Month }))
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

                var categoriasMes = new List<string>();
                var serieCompraEjec = new List<decimal>();
                var serieCompraPag = new List<decimal>();
                var serieServEjec = new List<decimal>();
                var serieServPag = new List<decimal>();

                // FUNCION LOCAL DE CONVERSIÓN
                decimal Convertir(decimal monto, int monedaOrigen, decimal tc)
                {
                    if (tc <= 0) tc = 1; // Evitar división por cero

                    if (monedaVisualizacion == 1) // QUIERO VER SOLES
                    {
                        if (monedaOrigen == 1) return monto;      // Soles -> Soles: Igual
                        if (monedaOrigen == 2) return monto * tc; // Dólares -> Soles: Multiplico
                    }
                    else // QUIERO VER DÓLARES
                    {
                        if (monedaOrigen == 2) return monto;      // Dólares -> Dólares: Igual
                        if (monedaOrigen == 1) return monto / tc; // Soles -> Dólares: Divido
                    }
                    return monto; // Default
                }

                foreach (var f in fechasUnicas)
                {
                    categoriasMes.Add(new DateTime(f.Year, f.Month, 1).ToString("MMM-yyyy", CultureInfo.CreateSpecificCulture("es-PE")));

                    // --- COMPRAS ---
                    var mesOC = dataOC.Where(x => x.FechaEmision.GetValueOrDefault().Year == f.Year && x.FechaEmision.GetValueOrDefault().Month == f.Month);
                    var mesPagC = dataPagosC.Where(x => x.FechaPago.Year == f.Year && x.FechaPago.Month == f.Month);

                    serieCompraEjec.Add(Math.Round(mesOC.Sum(x => Convertir(x.Total, x.MonedaId.GetValueOrDefault(), x.TC)), 2));
                    serieCompraPag.Add(Math.Round(mesPagC.Sum(x => Convertir(x.Monto, x.Moneda.GetValueOrDefault(), x.TC.GetValueOrDefault())), 2));

                    // --- SERVICIOS ---
                    var mesOS = dataOS.Where(x => x.FechaEmision.GetValueOrDefault().Year == f.Year && x.FechaEmision.GetValueOrDefault().Month == f.Month);
                    var mesPagS = dataPagosS.Where(x => x.FechaPago.Year == f.Year && x.FechaPago.Month == f.Month);

                    serieServEjec.Add(Math.Round(mesOS.Sum(x => Convertir(x.Total, x.MonedaId.GetValueOrDefault(), x.TC)), 2));
                    serieServPag.Add(Math.Round(mesPagS.Sum(x => Convertir(x.Monto, x.Moneda.GetValueOrDefault(), x.TC.GetValueOrDefault())), 2));
                }

                // =================================================================================
                // 5. RETORNO JSON
                // =================================================================================
                return Json(new
                {
                    status = true,
                    kpis = new
                    {
                        montoTotal = serieCompraEjec.Sum() + serieServEjec.Sum(),
                        montoPagado = serieCompraPag.Sum() + serieServPag.Sum(),
                        countOC = await qOC.CountAsync() + await qOS.CountAsync(),
                        countReq = await qRC.CountAsync() + await qRS.CountAsync()
                    },
                    charts = new
                    {
                        evolucionCategories = categoriasMes,
                        seriesCompra = new { ejecutado = serieCompraEjec, pagado = serieCompraPag },
                        seriesServicio = new { ejecutado = serieServEjec, pagado = serieServPag },
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