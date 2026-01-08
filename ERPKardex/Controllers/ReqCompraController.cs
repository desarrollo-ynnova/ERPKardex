using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class ReqCompraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReqCompraController(ApplicationDbContext context)
        {
            _context = context;
        }
        // --- EN ReqCompraController.cs ---

        // 1. VISTA INDEX
        public IActionResult Index()
        {
            return View();
        }

        // 2. API: LISTADO GENERAL
        [HttpGet]
        public JsonResult GetRequerimientosData()
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                var data = (from r in _context.ReqCompras
                            join usu in _context.Usuarios on r.UsuarioSolicitanteId equals usu.Id
                            join est in _context.Estados on r.EstadoId equals est.Id
                            where r.EmpresaId == empresaId
                            orderby r.Id descending // Lo más reciente primero
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

        // 3. API: VER DETALLE (PARA EL MODAL)
        [HttpGet]
        public async Task<JsonResult> GetDetalleReq(int id)
        {
            try
            {
                var cabecera = await _context.ReqCompras
                    .Where(x => x.Id == id)
                    .Select(x => new { x.Numero, x.Observacion })
                    .FirstOrDefaultAsync();

                var detalles = await _context.DReqCompras
                    .Where(x => x.ReqCompraId == id)
                    .Select(x => new
                    {
                        x.Item,
                        x.DescripcionProducto, // El snapshot que guardamos
                        x.UnidadMedida,
                        x.CantidadSolicitada
                    })
                    .ToListAsync();

                return Json(new { status = true, cabecera, detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        // GET: Vista de Registro
        public IActionResult Registrar()
        {
            // Aquí retornamos la vista que construiremos a continuación
            return View();
        }

        // GET: Listado de Productos para el combo (Select2)
        [HttpGet]
        public async Task<JsonResult> GetProductos()
        {
            try
            {
                var empresaIdStr = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdStr) ? int.Parse(empresaIdStr) : 0;

                // Solo traemos lo necesario: ID, Código, Nombre y UM (para el snapshot)
                var data = await _context.Productos
                    .Where(x => x.EmpresaId == empresaId && x.Estado == true)
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

        // POST: Guardar Requerimiento
        // ... imports

        // 1. MÉTODO GUARDAR (Corrección: Buscar ID de "Pendiente")
        [HttpPost]
        public JsonResult Guardar(ReqCompra cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                    var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    // --- A. BUSCAR ESTADO "PENDIENTE" DINÁMICAMENTE ---
                    var estadoPendiente = _context.Estados
                        .FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "REQ");

                    if (estadoPendiente == null)
                        throw new Exception("El estado 'Pendiente' para REQ no está configurado en la BD.");

                    // --- B. BUSCAR TIPO DOC ---
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "REQ");
                    if (tipoDoc == null) throw new Exception("Falta configurar documento REQ.");

                    // --- C. CORRELATIVO ---
                    var ultimo = _context.ReqCompras
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo)) nro = int.Parse(ultimo.Split('-')[1]) + 1;

                    // ASIGNACIÓN
                    cabecera.Numero = $"REQ-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.FechaRegistro = DateTime.Now;

                    // AQUI LA CORRECCIÓN: USAMOS EL ID CONSULTADO
                    cabecera.EstadoId = estadoPendiente.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now.AddDays(1);

                    _context.ReqCompras.Add(cabecera);
                    _context.SaveChanges();

                    // Detalles... (Igual que antes)
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var lista = JsonConvert.DeserializeObject<List<DReqCompra>>(detallesJson);
                        int item = 1;
                        foreach (var det in lista)
                        {
                            det.Id = 0;
                            det.ReqCompraId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = item.ToString("D3");
                            // Snapshot de productos...
                            var prod = _context.Productos.Find(det.ProductoId);
                            if (prod != null)
                            {
                                det.DescripcionProducto = prod.DescripcionProducto;
                                det.UnidadMedida = prod.CodUnidadMedida;
                            }
                            _context.DReqCompras.Add(det);
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

        // 2. MÉTODO CAMBIAR ESTADO (Corrección: Recibe string nombreEstado)
        [HttpPost]
        public JsonResult CambiarEstado(int id, string nombreEstado)
        {
            try
            {
                // 1. Buscar el ID del estado solicitado en la BD
                var estadoDb = _context.Estados
                    .FirstOrDefault(e => e.Nombre == nombreEstado && e.Tabla == "REQ");

                if (estadoDb == null)
                    return Json(new { status = false, message = $"El estado '{nombreEstado}' no existe en configuración." });

                // 2. Actualizar Requerimiento
                var req = _context.ReqCompras.Find(id);
                if (req == null) return Json(new { status = false, message = "Requerimiento no encontrado" });

                req.EstadoId = estadoDb.Id;
                _context.SaveChanges();

                return Json(new { status = true, message = $"Requerimiento {nombreEstado} correctamente" });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
    }
}