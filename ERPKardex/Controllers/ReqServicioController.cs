using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class ReqServicioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReqServicioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. VISTA INDEX
        public IActionResult Index()
        {
            return View();
        }

        // 2. LISTADO (Grilla Principal)
        [HttpGet]
        public JsonResult GetRequerimientosData()
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                var data = (from r in _context.ReqServicios
                            join usu in _context.Usuarios on r.UsuarioSolicitanteId equals usu.Id
                            join est in _context.Estados on r.EstadoId equals est.Id
                            where (empresaId == 4 || r.EmpresaId == empresaId)
                            orderby r.Id descending
                            select new
                            {
                                r.Id,
                                r.Numero,
                                FechaEmision = r.FechaEmision.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                FechaNecesaria = r.FechaNecesaria.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                Solicitante = usu.Nombre,
                                Estado = est.Nombre,
                                r.EstadoId,
                                r.Observacion
                            }).ToList();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // 3. DETALLE MODAL (Actualizado con Centro de Costo)
        [HttpGet]
        public async Task<JsonResult> GetDetalleReq(int id)
        {
            try
            {
                var cabecera = await _context.ReqServicios
                    .Where(x => x.Id == id)
                    .Select(x => new { x.Numero, x.Observacion })
                    .FirstOrDefaultAsync();

                // Hacemos Left Join con CentroCosto para mostrar el nombre en el modal
                var detalles = await (from d in _context.DReqServicios
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty() // Left join
                                      where d.ReqServicioId == id
                                      select new
                                      {
                                          d.Item,
                                          d.DescripcionServicio,
                                          d.CantidadSolicitada,
                                          d.Lugar,
                                          // Jalamos el nombre para visualización
                                          CentroCosto = cc != null ? cc.Nombre : "N/A"
                                      })
                                      .ToListAsync();

                return Json(new { status = true, cabecera, detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        public IActionResult Registrar()
        {
            return View();
        }

        // 4. LISTADO DE SERVICIOS (Para el combo de selección)
        [HttpGet]
        public async Task<JsonResult> GetServicios()
        {
            try
            {
                var empresaIdStr = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdStr) ? int.Parse(empresaIdStr) : 0;

                var data = await _context.Productos
                    .Where(x => (empresaId == 4 || x.EmpresaId == empresaId) && x.Estado == true)
                    // Filtro opcional si tienes un flag o código para servicios
                    // .Where(x => x.Tipo == "SERVICIO") 
                    .Select(x => new
                    {
                        x.Id,
                        x.Codigo,
                        x.DescripcionProducto,
                        x.DescripcionComercial,
                        x.CodUnidadMedida
                    })
                    .ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // 5. NUEVO: OBTENER CENTROS DE COSTO (Igual que en Compras)
        [HttpGet]
        public async Task<JsonResult> GetCentrosCosto()
        {
            try
            {
                var empresaIdStr = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdStr) ? int.Parse(empresaIdStr) : 0;

                var data = await _context.CentroCostos
                    .Where(x => (empresaId == 4 || x.EmpresaId == empresaId) && x.Estado == true && x.EsImputable == true)
                    .Select(x => new
                    {
                        x.Id,
                        x.Codigo,
                        x.Nombre
                    })
                    .OrderBy(x => x.Codigo)
                    .ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // 6. GUARDAR REQUERIMIENTO
        [HttpPost]
        public JsonResult Guardar(ReqServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                    var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    // A. Estado Pendiente
                    var estadoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "REQ");
                    if (estadoPendiente == null) throw new Exception("Estado Pendiente no configurado.");

                    // B. Tipo Doc RS (Requerimiento de Servicio)
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "RS");
                    if (tipoDoc == null) throw new Exception("Falta configurar documento RS.");

                    // C. Correlativo
                    var ultimo = _context.ReqServicios
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int correlativo))
                            nro = correlativo + 1;
                    }

                    // D. Guardar Cabecera
                    cabecera.Numero = $"RS-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estadoPendiente.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now.AddDays(1);

                    _context.ReqServicios.Add(cabecera);
                    _context.SaveChanges();

                    // E. Guardar Detalles
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DReqServicio>>(detallesJson);
                        int item = 1;
                        foreach (var det in lista)
                        {
                            det.Id = 0;
                            det.ReqServicioId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");

                            // Aseguramos que se guarde el CentroCostoId que viene del JSON
                            // det.CentroCostoId ya debe venir cargado.

                            _context.DReqServicios.Add(det);
                            item++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Requerimiento {cabecera.Numero} generado." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        // 7. CAMBIAR ESTADO
        [HttpPost]
        public JsonResult CambiarEstado(int id, string nombreEstado)
        {
            try
            {
                var estadoDb = _context.Estados
                    .FirstOrDefault(e => e.Nombre == nombreEstado && e.Tabla == "REQ");

                if (estadoDb == null)
                    return Json(new { status = false, message = $"Estado '{nombreEstado}' no encontrado." });

                var req = _context.ReqServicios.Find(id);
                if (req == null) return Json(new { status = false, message = "No encontrado" });

                req.EstadoId = estadoDb.Id;
                _context.SaveChanges();

                return Json(new { status = true, message = "Estado actualizado." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
    }
}