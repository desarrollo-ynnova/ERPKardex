using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class PedCompraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedCompraController(ApplicationDbContext context)
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

        // ==========================================
        // MÉTODOS PARA EL LISTADO (INDEX)
        // ==========================================

        [HttpGet]
        public async Task<JsonResult> GetPedidosCompraData()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaUsuario = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                // 1. Consulta base
                var query = from p in _context.PedCompras
                            join u in _context.Usuarios on p.UsuarioSolicitanteId equals u.Id
                            join e in _context.Estados on p.EstadoId equals e.Id
                            // NOTA: No hacemos join con CentroCosto aquí porque ya no está en cabecera
                            select new
                            {
                                p.Id,
                                p.EmpresaId,
                                p.Numero,
                                p.FechaEmision,
                                p.FechaNecesaria,
                                Solicitante = u.Nombre,
                                Estado = e.Nombre,

                                // 2. Subconsulta: Traemos los códigos de CC únicos asociados a este pedido
                                CentrosInvolucrados = (from d in _context.DPedidoCompras
                                                       join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                                       where d.PedidoCompraId == p.Id
                                                       select cc.Codigo).Distinct().ToList()
                            };

                // 3. Filtro de seguridad (Si no es Logístico-4, solo ve lo de su empresa)
                if (empresaUsuario != 4)
                {
                    query = query.Where(x => x.EmpresaId == empresaUsuario);
                }

                var listaRaw = await query.OrderByDescending(x => x.Numero).ToListAsync();

                // 4. Formateo final en memoria (Concatenamos los CC con comas)
                var data = listaRaw.Select(x => new
                {
                    x.Id,
                    x.Numero,
                    x.FechaEmision,
                    x.FechaNecesaria,
                    // Ejemplo de resultado: "ADM, VTA" o "LOG-01"
                    CentroCosto = x.CentrosInvolucrados.Any() ? string.Join(", ", x.CentrosInvolucrados) : "N/A",
                    x.Solicitante,
                    x.Estado
                });

                return Json(new { status = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error al cargar: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetDetallePedido(int id)
        {
            try
            {
                // Consulta detallada incluyendo el Centro de Costo por ítem
                var detalles = (from d in _context.DPedidoCompras
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where d.PedidoCompraId == id
                                select new
                                {
                                    d.Item,
                                    d.DescripcionLibre, // Usamos el campo correcto según tu script
                                    d.UnidadMedida,
                                    d.CantidadAprobada, // O CantidadSolicitada según tu lógica de visualización
                                    d.ObservacionItem,
                                    CentroCosto = cc.Codigo, // Código del CC específico
                                    Ref = d.TablaReferencia == "DREQCOMPRA" ? ("Req. " + d.ItemReferencia) : "-"
                                }).OrderBy(x => x.Item).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // ==========================================
        // 1. CARGA DE COMBOS Y DATOS MAESTROS
        // ==========================================
        [HttpGet]
        public JsonResult GetEmpresaData()
        {
            try
            {
                // Listamos todas las empresas para que el logístico elija a nombre de quién compra
                var data = _context.Empresas.Where(x => x.Estado == true).Select(x => new { x.Id, x.Ruc, x.RazonSocial }).ToList();
                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ==========================================
        // 2. BÚSQUEDA DE REQUERIMIENTOS APROBADOS
        // ==========================================
        [HttpGet]
        public JsonResult GetRequerimientosAprobados(int? empresaDestinoId)
        {
            try
            {
                var empresaUsuario = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                // Si soy logístico (4), filtro por la empresa seleccionada en el combo.
                // Si soy otro usuario, filtro por mi propia empresa.
                int idParaFiltrar = (empresaUsuario == 4) ? (empresaDestinoId ?? 0) : empresaUsuario;

                if (idParaFiltrar == 0) return Json(new { status = false, message = "Seleccione una empresa para buscar requerimientos." });

                var data = (from r in _context.ReqCompras
                            join u in _context.Usuarios on r.UsuarioSolicitanteId equals u.Id
                            join e in _context.Estados on r.EstadoId equals e.Id
                            where r.EmpresaId == idParaFiltrar
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

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // MÉTODO LIGERO PARA VER DETALLE EN MODAL (OJITO)
        [HttpGet]
        public JsonResult GetDetallesDeUnReq(int reqId)
        {
            try
            {
                var detalles = (from d in _context.DReqCompras
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where d.ReqCompraId == reqId
                                select new
                                {
                                    d.Item,
                                    d.DescripcionProducto,
                                    d.CantidadSolicitada,
                                    d.UnidadMedida,
                                    CentroCosto = cc.Codigo // Mostramos el CC en la vista previa
                                }).ToList();
                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ==========================================
        // 3. CARGA DE PRODUCTOS AL PEDIDO (MAPPING)
        // ==========================================
        [HttpPost]
        public JsonResult GetDetallesBatch([FromBody] List<int> reqIds)
        {
            try
            {
                if (reqIds == null || reqIds.Count == 0)
                    return Json(new { status = false, message = "Ningún requerimiento seleccionado" });

                // JALAMOS TODA LA INFO NECESARIA DE DREQCOMPRA
                // Incluyendo CentroCostoId y DescripcionProducto
                var detalles = (from d in _context.DReqCompras
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where reqIds.Contains(d.ReqCompraId)
                                select new
                                {
                                    // Datos para DPedCompra
                                    ProductoId = d.ProductoId,
                                    DescripcionLibre = d.DescripcionProducto, // Mapeamos DescripcionProducto -> DescripcionLibre
                                    UnidadMedida = d.UnidadMedida,
                                    Cantidad = d.CantidadSolicitada,
                                    CentroCostoId = d.CentroCostoId, // ¡IMPORTANTE! Jalamos el CC del detalle origen

                                    // Datos visuales extra
                                    NombreCentroCosto = cc.Codigo + " - " + cc.Nombre,
                                    Observacion = d.ObservacionItem,

                                    // Datos de Referencia (Para saber de dónde vino y validación)
                                    ReqOrigenId = d.ReqCompraId,
                                    IdReferencia = d.Id,   // ID primario de DReqCompra
                                    ItemReferencia = d.Item
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ==========================================
        // 4. GUARDAR PEDIDO (TRANSACCIONAL)
        // ==========================================
        [HttpPost]
        public JsonResult GuardarPedido(PedCompra cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                    int empresaId = cabecera.EmpresaId.GetValueOrDefault();

                    if (empresaId == 0) throw new Exception("Empresa inválida.");

                    // 1. CABECERA
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "PED");
                    if (tipoDoc == null) throw new Exception("Falta configurar documento PED.");

                    // Correlativo
                    var ultimo = _context.PedCompras
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var parts = ultimo.Split('-');
                        if (parts.Length > 1 && int.TryParse(parts[1], out int val)) nro = val + 1;
                    }

                    cabecera.Numero = $"PED-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;

                    // Estado Inicial
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "PED")
                                      ?? _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "PED");
                    cabecera.EstadoId = estadoGenerado.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now;

                    _context.PedCompras.Add(cabecera);
                    _context.SaveChanges();

                    // 2. DETALLES
                    List<int> reqInvolucrados = new List<int>();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DPedCompra>>(detallesJson);
                        int item = 1;

                        foreach (var det in lista)
                        {
                            det.Id = 0;
                            det.PedidoCompraId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");
                            det.CantidadAprobada = det.CantidadSolicitada;

                            // Mapeo de referencias
                            if (det.IdReferencia != null && det.IdReferencia > 0)
                            {
                                det.TablaReferencia = "DREQCOMPRA"; // Nombre de la tabla origen

                                // Consultamos el detalle origen para obtener el ReqId Padre
                                var dReq = _context.DReqCompras.AsNoTracking().FirstOrDefault(x => x.Id == det.IdReferencia);
                                if (dReq != null && !reqInvolucrados.Contains(dReq.ReqCompraId))
                                {
                                    reqInvolucrados.Add(dReq.ReqCompraId);
                                }
                            }

                            // Aseguramos que CentroCostoId y DescripcionLibre vengan del JSON
                            // (Ya deberían venir cargados desde el front gracias a GetDetallesBatch)

                            _context.DPedidoCompras.Add(det);
                            item++;
                        }
                        _context.SaveChanges();
                    }

                    // 3. ACTUALIZAR ESTADO DE REQUERIMIENTOS A "ATENDIDO"
                    if (reqInvolucrados.Count > 0)
                    {
                        var estAtendido = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido" && e.Tabla == "REQ");
                        if (estAtendido != null)
                        {
                            var reqs = _context.ReqCompras.Where(r => reqInvolucrados.Contains(r.Id)).ToList();
                            reqs.ForEach(r => r.EstadoId = estAtendido.Id);
                            _context.SaveChanges();
                        }
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Pedido {cabecera.Numero} generado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }
    }
}