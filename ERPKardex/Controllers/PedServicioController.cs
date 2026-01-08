using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class PedServicioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedServicioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Registrar()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetPedidosServicioData()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                var data = (from p in _context.PedServicios
                            join tdi in _context.TiposDocumentoInterno on p.TipoDocumentoInternoId equals tdi.Id
                            join cc in _context.CentroCostos on p.CentroCostoId equals cc.Id
                            join est in _context.Estados on p.EstadoId equals est.Id
                            join usu in _context.Usuarios on p.UsuarioSolicitanteId equals usu.Id into joinUsu
                            from u in joinUsu.DefaultIfEmpty()
                            where p.EmpresaId == empresaId
                            orderby p.FechaRegistro descending
                            select new
                            {
                                Id = p.Id,
                                Numero = p.Numero,
                                TipoDocumento = tdi.Codigo,
                                FechaEmision = p.FechaEmision.HasValue ? p.FechaEmision.Value.ToString("yyyy-MM-dd") : "-",
                                FechaNecesaria = p.FechaNecesaria.HasValue ? p.FechaNecesaria.Value.ToString("yyyy-MM-dd") : "-",
                                CentroCosto = cc.Nombre,
                                Solicitante = u != null ? u.Nombre : "Sistema",
                                Estado = est.Nombre,
                                EstadoId = p.EstadoId,
                                Observacion = p.Observacion
                            }).ToList();

                return Json(new { data = data, message = "Pedidos de servicio retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = Enumerable.Empty<object>(), message = ex.Message, status = false });
            }
        }

        // ==========================================================
        // NUEVOS MÉTODOS PARA JALAR REQUERIMIENTOS APROBADOS (MODAL)
        // ==========================================================

        [HttpGet]
        public JsonResult GetRequerimientosAprobados()
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                // Buscamos requerimientos que estén en estado 'Aprobado' para la tabla 'REQ'
                var data = (from r in _context.ReqServicios
                            join u in _context.Usuarios on r.UsuarioSolicitanteId equals u.Id
                            join e in _context.Estados on r.EstadoId equals e.Id
                            where r.EmpresaId == empresaId
                               && e.Nombre == "Aprobado"
                               && e.Tabla == "REQ"
                            orderby r.FechaRegistro descending
                            select new
                            {
                                r.Id,
                                r.Numero,
                                Fecha = r.FechaEmision.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                Solicitante = u.Nombre,
                                r.Observacion
                            }).ToList();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult GetDetallesBatch([FromBody] List<int> reqIds)
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                if (reqIds == null || reqIds.Count == 0)
                    return Json(new { status = false, message = "No se seleccionaron requerimientos." });

                var detalles = (from d in _context.DReqServicios
                                where reqIds.Contains(d.ReqServicioId)
                                   && d.EmpresaId == empresaId
                                select new
                                {
                                    ProductoId = d.ProductoId,
                                    Descripcion = d.DescripcionServicio,
                                    UnidadMedida = d.UnidadMedida ?? "UND",
                                    Cantidad = d.CantidadSolicitada,
                                    ObservacionItem = d.ObservacionItem,
                                    // Datos de Referencia (Para DPedServicio)
                                    ReferenciaId = d.Id,
                                    ReferenciaTabla = "DREQSERVICIO"
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ==========================================
        // MÉTODOS AUXILIARES (COMBOS)
        // ==========================================

        [HttpGet]
        public async Task<JsonResult> GetCentrosCosto()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            var data = await _context.CentroCostos
                .Where(x => x.EmpresaId == empresaId && x.Estado == true && x.EsImputable == true)
                .Select(x => new { x.Id, x.Nombre, x.Codigo })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        // ==========================================
        // GUARDAR PEDIDO SERVICIO (TRANSACCIONAL)
        // ==========================================
        [HttpPost]
        public JsonResult GuardarPedido(PedServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                    int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    if (empresaId == 0) throw new Exception("Sesión no válida.");

                    // 1. Obtener Estado 'Generado' o 'Pendiente' para Pedido
                    var estadoInicial = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "PED");
                    if (estadoInicial == null) estadoInicial = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "PED");

                    // 2. Generar Correlativo (PS)
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "PS");
                    if (tipoDoc == null) throw new Exception("No existe configuración para el documento 'PS'.");

                    var ultimoRegistro = _context.PedServicios
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;
                    if (!string.IsNullOrEmpty(ultimoRegistro))
                    {
                        var partes = ultimoRegistro.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int numeroActual))
                            nuevoCorrelativo = numeroActual + 1;
                    }

                    string numeroGenerado = $"PS-{nuevoCorrelativo.ToString("D10")}";

                    // 3. Llenar Cabecera
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.Numero = numeroGenerado;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.EstadoId = estadoInicial?.Id ?? 1;
                    cabecera.FechaRegistro = DateTime.Now;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now;

                    _context.PedServicios.Add(cabecera);
                    _context.SaveChanges();

                    // 4. Procesar Detalles
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DPedServicio>>(detallesJson);
                        int correlativoItem = 1;

                        foreach (var det in listaDetalles)
                        {
                            det.Id = 0;
                            det.PedidoServicioId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = correlativoItem.ToString("D3");

                            // Aquí se guardan los campos ReferenciaId y ReferenciaTabla que vienen del batch
                            _context.DPedidosServicio.Add(det);
                            correlativoItem++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Pedido de Servicio {numeroGenerado} registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            }
        }
    }
}