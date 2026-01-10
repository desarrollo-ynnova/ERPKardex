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

        public IActionResult Index() { return View(); }
        public IActionResult Registrar() { return View(); }

        // ==========================================
        // 1. DATA PARA EL LISTADO (INDEX)
        // ==========================================
        [HttpGet]
        public async Task<JsonResult> GetPedidosServicioData()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaUsuario = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                // 1. Consulta Base
                var query = from p in _context.PedServicios
                            join u in _context.Usuarios on p.UsuarioSolicitanteId equals u.Id
                            join e in _context.Estados on p.EstadoId equals e.Id
                            // NOTA: Sin join a CentroCosto en cabecera
                            select new
                            {
                                p.Id,
                                p.EmpresaId,
                                p.Numero,
                                p.FechaEmision,
                                p.FechaNecesaria,
                                Solicitante = u.Nombre,
                                Estado = e.Nombre,

                                // 2. Subconsulta: Traer códigos de CC desde los detalles (dpedservicio)
                                CentrosInvolucrados = (from d in _context.DPedidosServicio
                                                       join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                                       where d.PedidoServicioId == p.Id
                                                       select cc.Codigo).Distinct().ToList()
                            };

                if (empresaUsuario != 4)
                {
                    query = query.Where(x => x.EmpresaId == empresaUsuario);
                }

                var listaRaw = await query.OrderByDescending(x => x.Numero).ToListAsync();

                // 3. Formateo en memoria
                var data = listaRaw.Select(x => new
                {
                    x.Id,
                    x.Numero,
                    x.FechaEmision,
                    x.FechaNecesaria,
                    // Concatenamos CCs (Ej: "ADM, LOG")
                    CentroCosto = x.CentrosInvolucrados.Any() ? string.Join(", ", x.CentrosInvolucrados) : "N/A",
                    x.Solicitante,
                    x.Estado
                });

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallePedido(int id)
        {
            try
            {
                // Detalle específico para el Modal (Ojito)
                var detalles = (from d in _context.DPedidosServicio
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where d.PedidoServicioId == id
                                select new
                                {
                                    d.Item,
                                    // En servicio usamos DescripcionServicio
                                    Descripcion = d.DescripcionServicio,
                                    d.UnidadMedida,
                                    Cantidad = d.Cantidad,
                                    CantidadAtendida = d.CantidadAtendida ?? 0,
                                    CentroCosto = cc.Codigo,
                                    Ref = d.TablaReferencia == "DREQSERVICIO" ? ("Req. " + d.ItemReferencia) : "-"
                                }).OrderBy(x => x.Item).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ==========================================
        // 2. MÉTODOS PARA REGISTRAR (CARGA DE REQS)
        // ==========================================
        [HttpGet]
        public JsonResult GetRequerimientosAprobados(int? empresaDestinoId)
        {
            try
            {
                var empresaUsuario = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                int idParaFiltrar = (empresaUsuario == 4) ? (empresaDestinoId ?? 0) : empresaUsuario;

                if (idParaFiltrar == 0) return Json(new { status = false, message = "Seleccione una empresa." });

                var data = (from r in _context.ReqServicios
                            join u in _context.Usuarios on r.UsuarioSolicitanteId equals u.Id
                            join e in _context.Estados on r.EstadoId equals e.Id
                            where r.EmpresaId == idParaFiltrar
                               && e.Nombre == "Aprobado" && e.Tabla == "REQ"
                            orderby r.FechaRegistro descending
                            select new
                            {
                                r.Id,
                                r.Numero,
                                Fecha = r.FechaEmision.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                FechaNecesaria = r.FechaNecesaria.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                Solicitante = u.Nombre,
                                r.Observacion
                            }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesDeUnReq(int reqId)
        {
            // Vista rápida en el buscador
            try
            {
                var detalles = (from d in _context.DReqServicios
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where d.ReqServicioId == reqId
                                select new
                                {
                                    d.Item,
                                    d.DescripcionServicio,
                                    d.CantidadSolicitada,
                                    d.Lugar,
                                    CentroCosto = cc.Codigo
                                }).ToList();
                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult GetDetallesBatch([FromBody] List<int> reqIds)
        {
            try
            {
                if (reqIds == null || reqIds.Count == 0)
                    return Json(new { status = false, message = "Ningún requerimiento seleccionado" });

                // Mapeo crucial: DREQSERVICIO -> DPEDSERVICIO
                var detalles = (from d in _context.DReqServicios
                                join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id
                                where reqIds.Contains(d.ReqServicioId)
                                select new
                                {
                                    ProductoId = d.ProductoId,
                                    DescripcionServicio = d.DescripcionServicio,
                                    UnidadMedida = d.UnidadMedida ?? "UND",
                                    Cantidad = d.CantidadSolicitada,
                                    CentroCostoId = d.CentroCostoId, // Jalamos el ID para guardarlo

                                    // Visuales
                                    NombreCentroCosto = cc.Codigo + " - " + cc.Nombre,
                                    Lugar = d.Lugar,

                                    // Referencias
                                    ReqOrigenId = d.ReqServicioId,
                                    IdReferencia = d.Id,
                                    ItemReferencia = d.Item
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // ==========================================
        // 3. GUARDAR
        // ==========================================
        [HttpGet]
        public JsonResult GetEmpresaData()
        {
            var data = _context.Empresas.Where(x => x.Estado == true).Select(x => new { x.Id, x.Ruc, x.RazonSocial }).ToList();
            return Json(new { status = true, data });
        }

        [HttpPost]
        public JsonResult GuardarPedido(PedServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                    int empresaId = cabecera.EmpresaId.GetValueOrDefault();

                    if (empresaId == 0) throw new Exception("Empresa inválida.");

                    // 1. CABECERA
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "PS"); // PEDIDO SERVICIO
                    if (tipoDoc == null) throw new Exception("Falta configurar documento PS.");

                    var ultimo = _context.PedServicios
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var parts = ultimo.Split('-');
                        if (parts.Length > 1 && int.TryParse(parts[1], out int val)) nro = val + 1;
                    }

                    cabecera.Numero = $"PS-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;

                    var estadoGen = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "PED")
                                 ?? _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "PED");
                    cabecera.EstadoId = estadoGen?.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now;

                    _context.PedServicios.Add(cabecera);
                    _context.SaveChanges();

                    // 2. DETALLES
                    List<int> reqInvolucrados = new List<int>();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DPedServicio>>(detallesJson);
                        int item = 1;

                        foreach (var det in lista)
                        {
                            det.Id = 0;
                            det.PedidoServicioId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");

                            // Referencias
                            if (det.IdReferencia != null && det.IdReferencia > 0)
                            {
                                det.TablaReferencia = "DREQSERVICIO";
                                var dReq = _context.DReqServicios.AsNoTracking().FirstOrDefault(x => x.Id == det.IdReferencia);
                                if (dReq != null && !reqInvolucrados.Contains(dReq.ReqServicioId))
                                {
                                    reqInvolucrados.Add(dReq.ReqServicioId);
                                }
                            }
                            _context.DPedidosServicio.Add(det);
                            item++;
                        }
                        _context.SaveChanges();
                    }

                    // 3. ACTUALIZAR ESTADO DE REQS A 'ATENDIDO'
                    if (reqInvolucrados.Count > 0)
                    {
                        var estAtendido = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido" && e.Tabla == "REQ");
                        if (estAtendido != null)
                        {
                            var reqs = _context.ReqServicios.Where(r => reqInvolucrados.Contains(r.Id)).ToList();
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
        // ==========================================
        // 5. API PARA OBTENER SUCURSAL/ALMACÉN POR DEFECTO
        // ==========================================
        [HttpGet]
        public JsonResult GetDatosDefaultEmpresa(int empresaIdSeleccionada)
        {
            try
            {
                // 1. Buscamos la Sucursal Principal (Por código '001' o nombre)
                var sucursal = _context.Sucursales
                    .FirstOrDefault(s => s.EmpresaId == empresaIdSeleccionada
                                      && s.Estado == true
                                      && (s.Codigo == "001" || s.Nombre.Contains("PRINCIPAL")));

                int? sucursalId = sucursal?.Id;
                string sucursalNombre = sucursal?.Nombre ?? "";

                int? almacenId = null;
                string almacenNombre = "";

                // 2. Si encontramos sucursal, buscamos su Almacén Principal
                if (sucursalId != null)
                {
                    var almacen = _context.Almacenes
                        .FirstOrDefault(a => a.EmpresaId == empresaIdSeleccionada
                                          && a.SucursalId == sucursalId
                                          && a.Estado == true
                                          && (a.Codigo == "01" || a.Nombre.Contains("PRINCIPAL")));

                    almacenId = almacen?.Id;
                    almacenNombre = almacen?.Nombre ?? "";
                }

                return Json(new
                {
                    status = true,
                    sucursalId,
                    sucursalNombre,
                    almacenId,
                    almacenNombre
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
    }
}