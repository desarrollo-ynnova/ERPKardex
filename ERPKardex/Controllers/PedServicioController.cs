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

                            // Left Join con Usuario
                            join usu in _context.Usuarios on p.UsuarioSolicitanteId equals usu.Id into joinUsu
                            from u in joinUsu.DefaultIfEmpty()

                            where p.EmpresaId == empresaId
                            orderby p.FechaRegistro descending

                            select new
                            {
                                Id = p.Id,
                                Numero = p.Numero,
                                TipoDocumento = tdi.Codigo, // "PS"
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

        // ==========================================
        // MÉTODOS AUXILIARES (COMBOS)
        // ==========================================
        [HttpGet]
        public async Task<JsonResult> GetSucursales()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            var data = await _context.Sucursales
                .Where(x => x.EmpresaId == empresaId && x.Estado == true)
                .Select(x => new { x.Id, x.Nombre })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetCentrosCosto()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            var data = await _context.CentroCostos
                .Where(x => x.EsImputable == true && x.EmpresaId == empresaId && x.Estado == true && x.EsImputable == true)
                .Select(x => new { x.Id, x.Nombre, x.Codigo })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetActividades()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
            var data = await _context.Actividades
                .Where(x => x.EmpresaId == empresaId && x.Estado == true)
                .Select(x => new { x.Id, x.Nombre, x.Codigo })
                .ToListAsync();
            return Json(new { status = true, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetProductosServicio()
        {
            var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

            // NOTA: Aquí podrías filtrar por .StartsWith("6") si quisieras ser estricto,
            // pero como pediste "listar todos", traemos todo el catálogo.
            var data = await _context.Productos
                .Where(x => x.EmpresaId == empresaId && x.Estado == true)
                .Select(x => new { x.Id, x.DescripcionProducto, x.DescripcionComercial, x.Codigo, x.CodUnidadMedida })
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
                    // 1. Datos de Sesión
                    var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                    int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    if (empresaId == 0) throw new Exception("Sesión no válida.");

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
                        {
                            nuevoCorrelativo = numeroActual + 1;
                        }
                    }

                    string numeroGenerado = $"PS-{nuevoCorrelativo.ToString("D10")}";

                    // 3. Llenar Cabecera (Sin lugar ni proveedor sugerido)
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.Numero = numeroGenerado;
                    cabecera.UsuarioSolicitanteId = usuarioId;
                    cabecera.EmpresaId = empresaId;
                    cabecera.EstadoId = 1; // Pendiente
                    cabecera.FechaRegistro = DateTime.Now;

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

                            // Aseguramos guardar el nombre del servicio/producto como snapshot
                            // (Aunque ya venga del front, es bueno validarlo o dejarlo como viene)
                            // det.DescripcionServicio viene lleno desde la vista con el nombre del producto

                            _context.DPedidosServicio.Add(det);
                            correlativoItem++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Solicitud de Servicio {numeroGenerado} registrada correctamente." });
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