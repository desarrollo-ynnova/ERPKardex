using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // HEREDAMOS DE BASECONTROLLER (Asegúrate de haber creado el archivo BaseController.cs)
    public class ReqCompraController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ReqCompraController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region 1. VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        #endregion

        #region 2. LISTADOS (GET)

        // LISTA PRINCIPAL (GRID)
        [HttpGet]
        public JsonResult GetRequerimientosData()
        {
            try
            {
                // Usamos propiedades de la clase base
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var data = (from r in _context.ReqCompras
                            join usu in _context.Usuarios on r.UsuarioSolicitanteId equals usu.Id
                            join est in _context.Estados on r.EstadoId equals est.Id
                            // LÓGICA LIMPIA: Si soy global veo todo, si no, solo lo mío
                            where (esGlobal || r.EmpresaId == miEmpresaId)
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

        // DETALLE PARA EL MODAL
        [HttpGet]
        public async Task<JsonResult> GetDetalleReq(int id)
        {
            try
            {
                var cabecera = await _context.ReqCompras
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Numero,
                        x.Observacion,
                        FechaNecesaria = x.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy")
                    })
                    .FirstOrDefaultAsync();

                // Join con Centro Costo y Estado
                var detalles = await (from d in _context.DReqCompras
                                      join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into ccJoin
                                      from cc in ccJoin.DefaultIfEmpty()
                                      join est in _context.Estados on d.EstadoId equals est.Id into estJoin
                                      from est in estJoin.DefaultIfEmpty()
                                      where d.ReqCompraId == id
                                      select new
                                      {
                                          d.Item,
                                          d.DescripcionProducto,
                                          d.UnidadMedida,
                                          d.Lugar, // Campo nuevo
                                          d.CantidadSolicitada,
                                          CentroCosto = cc != null ? cc.Nombre : "N/A",
                                          Estado = est != null ? est.Nombre : "-" // Estado del ítem
                                      }).ToListAsync();

                return Json(new { status = true, cabecera, detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // COMBO DE PRODUCTOS (Filtrado por Empresa)
        [HttpGet]
        public async Task<JsonResult> GetProductos()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var data = await _context.Productos
                    .Where(x => (esGlobal || x.EmpresaId == miEmpresaId) && x.Estado == true && !x.Codigo.StartsWith("6"))
                    .Select(x => new { x.Id, x.Codigo, x.DescripcionProducto, x.DescripcionComercial, x.CodUnidadMedida })
                    .ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // COMBO DE CENTROS DE COSTO (Filtrado por Empresa)
        [HttpGet]
        public async Task<JsonResult> GetCentrosCosto()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var data = await _context.CentroCostos
                    .Where(x => x.EmpresaId == miEmpresaId && x.Estado == true && x.EsImputable == true)
                    .Select(x => new { x.Id, x.Codigo, x.Nombre })
                    .OrderBy(x => x.Codigo)
                    .ToListAsync();

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. TRANSACCIONES (POST)

        // GUARDAR REQUERIMIENTO
        [HttpPost]
        public JsonResult Guardar(ReqCompra cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Usamos propiedades de la clase base para Empresa
                    // Para usuario seguimos usando Claims porque es dato específico de auditoría
                    var empresaId = EmpresaUsuarioId;
                    var usuarioId = UsuarioActualId;

                    // 1. ESTADOS
                    var estadoPendienteREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "REQ");
                    var estadoPendienteDREQ = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DREQ");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "REQ");

                    if (estadoPendienteREQ == null || estadoPendienteDREQ == null || tipoDoc == null)
                        throw new Exception("Faltan configurar estados o documentos (REQ).");

                    // 2. CORRELATIVO
                    var ultimo = _context.ReqCompras
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int correlativo)) nro = correlativo + 1;
                    }

                    // 3. CABECERA
                    cabecera.Numero = $"REQ-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.UsuarioRegistro = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estadoPendienteREQ.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now.AddDays(1);

                    _context.ReqCompras.Add(cabecera);
                    _context.SaveChanges();

                    // 4. DETALLES
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
                            det.EstadoId = estadoPendienteDREQ.Id; // Estado inicial del ítem

                            var prod = _context.Productos.Find(det.ProductoId);
                            if (prod != null)
                            {
                                det.DescripcionProducto = prod.DescripcionComercial;
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

        // CAMBIAR ESTADO (Aprobar/Rechazar)
        [HttpPost]
        public JsonResult CambiarEstado(int id, string nombreEstado)
        {
            try
            {
                var usuarioId = UsuarioActualId; // Usamos propiedad base
                var estadoDb = _context.Estados.FirstOrDefault(e => e.Nombre == nombreEstado && e.Tabla == "REQ");

                if (estadoDb == null) return Json(new { status = false, message = "Estado no configurado." });

                var req = _context.ReqCompras.Find(id);
                if (req == null) return Json(new { status = false, message = "No encontrado." });

                req.EstadoId = estadoDb.Id;
                req.UsuarioAprobador = usuarioId;
                req.FechaAprobacion = DateTime.Now;
                _context.SaveChanges();

                return Json(new { status = true, message = $"Requerimiento {nombreEstado} correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
    }
}