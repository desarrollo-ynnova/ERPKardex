using ERPKardex.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

                var fInicio = fechaInicio ?? DateTime.Now.AddDays(-30);
                var fFin = fechaFin ?? DateTime.Now;

                // =================================================================================
                // 1. DATA ÓRDENES (COMPRAS + SERVICIOS) - Para Montos
                // =================================================================================
                var qOC = _context.OrdenCompras.AsQueryable();
                var qOS = _context.OrdenServicios.AsQueryable();

                if (idEmpresaFiltro > 0)
                {
                    qOC = qOC.Where(x => x.EmpresaId == idEmpresaFiltro);
                    qOS = qOS.Where(x => x.EmpresaId == idEmpresaFiltro);
                }

                qOC = qOC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                qOS = qOS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);

                // Proyección ligera
                var dataOC = await qOC.Select(x => new { x.Id, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1, x.EstadoId, Tipo = "COMPRA", x.FechaEmision }).ToListAsync();
                var dataOS = await qOS.Select(x => new { x.Id, Total = x.Total ?? 0, x.MonedaId, TC = x.TipoCambio ?? 1, x.EstadoId, Tipo = "SERVICIO", x.FechaEmision }).ToListAsync();

                // Unificamos para facilitar cálculos
                var dataTotal = dataOC.Concat(dataOS).ToList();

                decimal totalSoles = dataTotal.Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);
                decimal totalCompras = dataOC.Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);
                decimal totalServicios = dataOS.Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total);

                // =================================================================================
                // 2. DATA REQUERIMIENTOS (COMPRAS + SERVICIOS) - Para Eficiencia
                // =================================================================================
                // Obtenemos los estados clave para clasificar
                var estadosReq = await _context.Estados.Where(e => e.Tabla == "REQ").ToListAsync();

                // Helper local para clasificar estado
                string ClasificarEstado(int estadoId)
                {
                    var nombre = estadosReq.FirstOrDefault(e => e.Id == estadoId)?.Nombre ?? "";
                    if (nombre.Contains("Atendido")) return "ATENDIDO";
                    if (nombre.Contains("Rechazado") || nombre.Contains("Anulado")) return "CANCELADO";
                    return "PENDIENTE"; // Pendiente, Aprobado
                }

                // A. Requerimientos de Compra
                var qRC = _context.ReqCompras.AsQueryable();
                if (idEmpresaFiltro > 0) qRC = qRC.Where(x => x.EmpresaId == idEmpresaFiltro);
                qRC = qRC.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                var rawRC = await qRC.Select(x => x.EstadoId).ToListAsync();

                var kpiRC = rawRC.GroupBy(id => ClasificarEstado(id))
                                 .Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToList();

                // B. Requerimientos de Servicio
                var qRS = _context.ReqServicios.AsQueryable();
                if (idEmpresaFiltro > 0) qRS = qRS.Where(x => x.EmpresaId == idEmpresaFiltro);
                qRS = qRS.Where(x => x.FechaEmision >= fInicio && x.FechaEmision <= fFin);
                var rawRS = await qRS.Select(x => x.EstadoId).ToListAsync();

                var kpiRS = rawRS.GroupBy(id => ClasificarEstado(id))
                                 .Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToList();

                // =================================================================================
                // 3. KPI PAGOS (Financiero)
                // =================================================================================
                // Buscamos pagos relacionados a las órdenes de este periodo
                var idsOC = dataOC.Select(x => x.Id).ToList();
                var idsOS = dataOS.Select(x => x.Id).ToList();

                var pagos = await _context.OrdenPagos
                    .Where(p => (p.OrdenCompraId.HasValue && idsOC.Contains(p.OrdenCompraId.Value)) ||
                                (p.OrdenServicioId.HasValue && idsOS.Contains(p.OrdenServicioId.Value)))
                    .SumAsync(p => p.MontoAbonado); // Asumiendo todo a Soles para simplificar KPI

                // =================================================================================
                // 4. PREPARAR CHART DATA
                // =================================================================================

                // Chart 1: Evolución de Órdenes (Barras apiladas Compra vs Servicio)
                var fechas = dataTotal.Select(x => x.FechaEmision.Value.ToString("dd/MM")).Distinct().OrderBy(x => x).ToList();
                var serieCompra = new List<decimal>();
                var serieServicio = new List<decimal>();

                foreach (var f in fechas)
                {
                    serieCompra.Add(dataOC.Where(x => x.FechaEmision.Value.ToString("dd/MM") == f)
                                        .Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total));
                    serieServicio.Add(dataOS.Where(x => x.FechaEmision.Value.ToString("dd/MM") == f)
                                        .Sum(x => x.MonedaId == 2 ? x.Total * x.TC : x.Total));
                }

                // Chart 2 y 3 (Donuts de Eficiencia) -> Ya los tenemos en kpiRC y kpiRS

                return Json(new
                {
                    status = true,
                    kpis = new
                    {
                        montoTotal = totalSoles,
                        montoCompras = totalCompras,
                        montoServicios = totalServicios,
                        montoPagado = pagos,
                        countReqTotal = rawRC.Count + rawRS.Count
                    },
                    charts = new
                    {
                        evolucion = new { categories = fechas, compras = serieCompra, servicios = serieServicio },
                        reqCompras = kpiRC.Select(x => new { name = x.Estado, value = x.Cantidad }),
                        reqServicios = kpiRS.Select(x => new { name = x.Estado, value = x.Cantidad })
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