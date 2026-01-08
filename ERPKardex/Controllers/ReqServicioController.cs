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

        // --- EN ReqServicioController.cs ---

        // 1. VISTA INDEX
        public IActionResult Index()
        {
            return View();
        }

        // 2. LISTADO
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

        // 3. DETALLE MODAL
        [HttpGet]
        public async Task<JsonResult> GetDetalleReq(int id)
        {
            try
            {
                var cabecera = await _context.ReqServicios
                    .Where(x => x.Id == id)
                    .Select(x => new { x.Numero, x.Observacion })
                    .FirstOrDefaultAsync();

                var detalles = await _context.DReqServicios
                    .Where(x => x.ReqServicioId == id)
                    .Select(x => new
                    {
                        x.Item,
                        x.DescripcionServicio, // Descripción del servicio
                        x.CantidadSolicitada,
                        x.ObservacionItem,
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

        // Listado de Servicios (Productos con código 6 o todo el catálogo según tu lógica)
        [HttpGet]
        public async Task<JsonResult> GetServicios()
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                // Traemos data para llenar el combo
                var data = await _context.Productos
                    .Where(x => x.EmpresaId == empresaId && x.Estado == true)
                    // Si deseas filtrar solo servicios: && x.Codigo.StartsWith("6")
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

        [HttpPost]
        public JsonResult Guardar(ReqServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                    var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    // 1. Estado Pendiente
                    var estadoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "REQ");
                    if (estadoPendiente == null) throw new Exception("Estado Pendiente no configurado.");

                    // 2. Tipo Doc RS
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "RS");
                    if (tipoDoc == null) throw new Exception("Falta configurar documento RS.");

                    // 3. Correlativo
                    var ultimo = _context.ReqServicios
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo)) nro = int.Parse(ultimo.Split('-')[1]) + 1;

                    // 4. Guardar Cabecera
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

                    // 5. Guardar Detalles (TAL CUAL VIENEN DEL FRONT)
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

                            // IMPORTANTE: Aquí NO sobreescribimos descripciones. 
                            // Se guarda lo que mandó el JS en 'DescripcionServicio' y 'ObservacionItem'

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

        [HttpPost]
        public JsonResult CambiarEstado(int id, string nombreEstado)
        {
            try
            {
                // 1. Buscar ID por nombre
                var estadoDb = _context.Estados
                    .FirstOrDefault(e => e.Nombre == nombreEstado && e.Tabla == "REQ");

                if (estadoDb == null)
                    return Json(new { status = false, message = $"Estado '{nombreEstado}' no encontrado." });

                // 2. Actualizar
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