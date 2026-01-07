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
            return View(); // Lo haremos luego
        }

        public IActionResult Registrar()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetPedidosCompraData()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                var data = (from p in _context.PedCompras
                            join tdi in _context.TiposDocumentoInterno on p.TipoDocumentoInternoId equals tdi.Id
                            join suc in _context.Sucursales on p.SucursalId equals suc.Id
                            join cc in _context.CentroCostos on p.CentroCostoId equals cc.Id
                            join est in _context.Estados on p.EstadoId equals est.Id

                            // Left Join con Usuario Solicitante (por si es nulo)
                            join usu in _context.Usuarios on p.UsuarioSolicitanteId equals usu.Id into joinUsu
                            from u in joinUsu.DefaultIfEmpty()

                            where p.EmpresaId == empresaId
                            orderby p.FechaRegistro descending // Ordenar por fecha reciente

                            select new
                            {
                                Id = p.Id,
                                Numero = p.Numero,
                                TipoDocumento = tdi.Codigo, // "PED"
                                FechaEmision = p.FechaEmision.HasValue ? p.FechaEmision.Value.ToString("yyyy-MM-dd") : "-",
                                FechaNecesaria = p.FechaNecesaria.HasValue ? p.FechaNecesaria.Value.ToString("yyyy-MM-dd") : "-",

                                Sucursal = suc.Nombre,
                                CentroCosto = cc.Nombre,
                                Solicitante = u != null ? u.Nombre : "Sistema",

                                Estado = est.Nombre,
                                EstadoId = p.EstadoId, // Útil para pintar badges de colores en el front
                                Observacion = p.Observacion
                            }).ToList();

                return Json(new { data = data, message = "Pedidos de compra retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = Enumerable.Empty<object>(), message = ex.Message, status = false });
            }
        }

        // ==========================================
        // MÉTODOS AUXILIARES (COMBOS)
        // ==========================================
        [HttpGet]
        public async Task<JsonResult> GetSucursales()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            var data = await _context.Sucursales
                .Where(x => x.EmpresaId == empresaId && x.Estado == true)
                .Select(x => new { x.Id, x.Nombre, x.Codigo })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetCentrosCosto()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            // Filtramos solo los imputables (último nivel)
            var data = await _context.CentroCostos
                .Where(x => x.EmpresaId == empresaId && x.Estado == true && x.EsImputable == true)
                .Select(x => new { x.Id, x.Nombre, x.Codigo })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetProductos()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            var data = await _context.Productos
                .Where(x => x.EmpresaId == empresaId && x.Estado == true)
                .Select(x => new { x.Id, x.DescripcionProducto, x.Codigo, x.CodUnidadMedida })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        // ==========================================
        // GUARDAR PEDIDO (TRANSACCIONAL)
        // ==========================================
        [HttpPost]
        public JsonResult GuardarPedido(PedCompra cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Datos de Sesión
                    var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                    int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    if (empresaId == 0) throw new Exception("Sesión no válida.");

                    // 2. Generar Correlativo (Lógica PED)
                    // Buscamos el documento interno 'PED'
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "PED");
                    if (tipoDoc == null) throw new Exception("No existe configuración para el documento 'PED'.");

                    // Consultamos el último número usado PARA ESTA EMPRESA
                    var ultimoRegistro = _context.PedCompras
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;
                    if (!string.IsNullOrEmpty(ultimoRegistro))
                    {
                        // Formato esperado: PED-0000000001
                        var partes = ultimoRegistro.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int numeroActual))
                        {
                            nuevoCorrelativo = numeroActual + 1;
                        }
                    }

                    string numeroGenerado = $"PED-{nuevoCorrelativo.ToString("D10")}";

                    // 3. Llenar Cabecera
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.Numero = numeroGenerado;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.EstadoId = 1; // 1: Pendiente
                    cabecera.FechaRegistro = DateTime.Now;

                    // Si la fecha necesaria no viene, ponemos la misma de emisión (o validamos)
                    if (cabecera.FechaNecesaria == DateTime.MinValue) cabecera.FechaNecesaria = DateTime.Now;

                    _context.PedCompras.Add(cabecera);
                    _context.SaveChanges(); // Obtenemos ID

                    // 4. Procesar Detalles
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DPedCompra>>(detallesJson);
                        int correlativoItem = 1;

                        foreach (var det in listaDetalles)
                        {
                            det.Id = 0;
                            det.PedidoCompraId = cabecera.Id;
                            det.EmpresaId = empresaId;
                            det.Item = correlativoItem.ToString("D3"); // 001, 002...

                            // Recuperar snapshot de unidad de medida del producto
                            var prod = _context.Productos.Find(det.ProductoId);
                            if (prod != null)
                            {
                                det.UnidadMedida = prod.CodUnidadMedida;
                            }

                            // Al registrar, la cantidad aprobada suele ser igual a la solicitada por defecto
                            // o 0 si requiere flujo de aprobación estricto. La pondremos igual por ahora.
                            det.CantidadAprobada = det.CantidadSolicitada;

                            _context.DPedidoCompras.Add(det);
                            correlativoItem++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Pedido {numeroGenerado} registrado correctamente." });
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