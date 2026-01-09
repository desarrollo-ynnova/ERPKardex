using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class OrdenServicioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdenServicioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();
        public IActionResult Registrar() => View();

        #region 1. LISTADO (INDEX)

        [HttpGet]
        public JsonResult GetOrdenesData()
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                var query = from o in _context.OrdenServicios
                            join ent in _context.Entidades on o.EntidadId equals ent.Id
                            join est in _context.Estados on o.EstadoId equals est.Id
                            join mon in _context.Monedas on o.MonedaId equals mon.Id
                            where (empresaId == 4 || o.EmpresaId == empresaId)
                            orderby o.Id descending
                            select new
                            {
                                o.Id,
                                o.Numero,
                                Fecha = o.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                Proveedor = ent.RazonSocial,
                                Ruc = ent.Ruc,
                                Moneda = mon.Codigo,
                                Total = o.Total,
                                Estado = est.Nombre,
                                o.EstadoId,
                                o.Observacion
                            };

                return Json(new { status = true, data = query.ToList() });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetalleOrden(int id)
        {
            try
            {
                var detalles = (from d in _context.DOrdenServicios
                                join p in _context.Productos on d.ProductoId equals p.Id
                                where d.OrdenServicioId == id
                                select new
                                {
                                    d.Item,
                                    Producto = d.Descripcion, // Descripción larga
                                    d.UnidadMedida,
                                    d.Cantidad,
                                    d.PrecioUnitario,
                                    d.Total,
                                    d.TablaReferencia,
                                    d.IdReferencia
                                }).ToList();

                return Json(new { status = true, data = detalles });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 2. DATOS PARA REGISTRO

        [HttpGet]
        public JsonResult GetCombosRegistro()
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                var proveedores = _context.Entidades.Where(x => x.Estado == true && (empresaId == 4 || x.EmpresaId == empresaId))
                    .Select(x => new { x.Id, x.Ruc, x.RazonSocial }).ToList();

                var monedas = _context.Monedas.Where(x => x.Estado == true).ToList();

                return Json(new { status = true, proveedores, monedas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. LÓGICA DE JALAR PEDIDOS DE SERVICIO

        [HttpGet]
        public JsonResult GetPedidosPendientes(int? empresaFiltroId)
        {
            try
            {
                var empresaUsuario = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                int idEmpresa = (empresaUsuario == 4) ? (empresaFiltroId ?? 0) : empresaUsuario;

                var estadosValidos = _context.Estados.Where(e => (e.Nombre == "Generado" || e.Nombre == "Aprobado") && e.Tabla == "PED")
                                                     .Select(e => e.Id).ToList();

                var pedidos = (from p in _context.PedServicios
                               join d in _context.DPedidosServicio on p.Id equals d.PedidoServicioId
                               where p.EmpresaId == idEmpresa
                                     && estadosValidos.Contains(p.EstadoId ?? 0)
                                     && d.Cantidad > (d.CantidadAtendida ?? 0)
                               group p by new { p.Id, p.Numero, p.FechaEmision, p.Observacion } into g
                               select new
                               {
                                   g.Key.Id,
                                   g.Key.Numero,
                                   Fecha = g.Key.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                   g.Key.Observacion
                               }).ToList();

                return Json(new { status = true, data = pedidos });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetCabeceraPedido(int pedidoId)
        {
            try
            {
                var pedido = _context.PedServicios.Where(p => p.Id == pedidoId)
                    .Select(p => new
                    {
                        p.SucursalId,
                        p.LugarDestino,
                        SucursalNombre = _context.Sucursales.Where(s => s.Id == p.SucursalId).Select(s => s.Nombre).FirstOrDefault()
                    }).FirstOrDefault();

                return Json(new { status = true, data = pedido });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDetallesPedidoParaOrden(int pedidoId)
        {
            try
            {
                // 1. Items del Pedido
                var itemsPedido = (from d in _context.DPedidosServicio
                                   join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into joinCC
                                   from cc in joinCC.DefaultIfEmpty()
                                   where d.PedidoServicioId == pedidoId
                                   select new
                                   {
                                       d.Id,
                                       d.ProductoId,
                                       d.DescripcionServicio,
                                       d.UnidadMedida,
                                       CantidadSolicitada = d.Cantidad,
                                       d.CantidadAtendida,
                                       d.CentroCostoId,
                                       CentroCostoNombre = cc != null ? cc.Codigo : "-",
                                       d.Item
                                   }).ToList();

                // 2. CORRECCIÓN: Saldo Comprometido en Órdenes GENERADAS
                var estadoGeneradoOS = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN")?.Id ?? 0;

                var comprometidos = (from doc in _context.DOrdenServicios
                                     join os in _context.OrdenServicios on doc.OrdenServicioId equals os.Id
                                     where os.EstadoId == estadoGeneradoOS // <--- VALIDACIÓN CONTRA GENERADO
                                        && doc.TablaReferencia == "DPEDSERVICIO"
                                     select new { doc.IdReferencia, doc.Cantidad })
                                     .ToList();

                // 3. Cruzar y Calcular
                var resultado = itemsPedido.Select(ip =>
                {
                    decimal cantComprometida = comprometidos.Where(x => x.IdReferencia == ip.Id).Sum(x => x.Cantidad ?? 0);
                    decimal saldoDisponible = (ip.CantidadSolicitada ?? 0) - ((ip.CantidadAtendida ?? 0) + cantComprometida);

                    return new
                    {
                        ip.Id,
                        ip.ProductoId,
                        ip.Item,
                        Descripcion = ip.DescripcionServicio,
                        ip.UnidadMedida,
                        ip.CentroCostoId,
                        ip.CentroCostoNombre,
                        CantidadPedido = ip.CantidadSolicitada,
                        SaldoDisponible = saldoDisponible > 0 ? saldoDisponible : 0
                    };
                })
                .Where(x => x.SaldoDisponible > 0)
                .ToList();

                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 4. GUARDAR Y APROBAR (OS)

        [HttpPost]
        public JsonResult Guardar(OrdenServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaUsuario = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    // CORRECCIÓN: Usar estado GENERADO
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "OS");

                    if (estadoGenerado == null || tipoDoc == null)
                        throw new Exception("Falta configuración de estados (Generado) o documentos (OS).");

                    var ultimo = _context.OrdenServicios
                        .Where(x => x.EmpresaId == cabecera.EmpresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int val)) nro = val + 1;
                    }

                    cabecera.Numero = $"OS-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioCreacionId = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estadoGenerado.Id; // <--- ASIGNAR GENERADO

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;

                    _context.OrdenServicios.Add(cabecera);
                    _context.SaveChanges();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var items = JsonConvert.DeserializeObject<List<DOrdenServicio>>(detallesJson);
                        int itemCounter = 1;

                        foreach (var det in items)
                        {
                            if (det.IdReferencia != null && det.TablaReferencia == "DPEDSERVICIO")
                            {
                                var pedItem = _context.DPedidosServicio.AsNoTracking().FirstOrDefault(x => x.Id == det.IdReferencia);
                                if (pedItem != null)
                                {
                                    // Validar contra otras GENERADAS
                                    var emitidosOtros = _context.DOrdenServicios
                                        .Where(x => x.IdReferencia == det.IdReferencia && x.TablaReferencia == "DPEDSERVICIO"
                                                 && x.OrdenServicioId != cabecera.Id
                                                 && _context.OrdenServicios.Any(o => o.Id == x.OrdenServicioId && o.EstadoId == estadoGenerado.Id))
                                        .Sum(x => x.Cantidad ?? 0);

                                    decimal saldoReal = (pedItem.Cantidad ?? 0) - (pedItem.CantidadAtendida ?? 0) - emitidosOtros;

                                    if ((det.Cantidad ?? 0) > saldoReal)
                                        throw new Exception($"El servicio {det.Descripcion} excede el saldo disponible ({saldoReal}).");
                                }
                            }

                            det.Id = 0;
                            det.OrdenServicioId = cabecera.Id;
                            det.EmpresaId = cabecera.EmpresaId;
                            det.Item = itemCounter.ToString("D3");

                            if (cabecera.IncluyeIgv == false)
                            {
                                det.Impuesto = 0;
                                det.ValorVenta = det.Total;
                            }

                            _context.DOrdenServicios.Add(det);
                            itemCounter++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Orden de Servicio {cabecera.Numero} generada." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult AprobarOrden(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var orden = _context.OrdenServicios.Find(id);
                    if (orden == null) throw new Exception("Orden no encontrada.");

                    var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");

                    // CORRECCIÓN: Validar contra GENERADO
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");

                    if (orden.EstadoId != estadoGenerado.Id) throw new Exception("Solo se pueden aprobar órdenes en estado Generado.");

                    var detalles = _context.DOrdenServicios.Where(d => d.OrdenServicioId == id).ToList();

                    // ACTUALIZAR SALDOS PEDIDO SERVICIO
                    foreach (var det in detalles)
                    {
                        if (det.IdReferencia != null && det.TablaReferencia == "DPEDSERVICIO")
                        {
                            var pedItem = _context.DPedidosServicio.Find(det.IdReferencia);
                            if (pedItem != null)
                            {
                                decimal saldoActual = (pedItem.Cantidad ?? 0) - (pedItem.CantidadAtendida ?? 0);
                                if ((det.Cantidad ?? 0) > saldoActual)
                                    throw new Exception($"Conflicto de saldo en item {det.Item}.");

                                pedItem.CantidadAtendida = (pedItem.CantidadAtendida ?? 0) + (det.Cantidad ?? 0);
                            }
                        }
                    }
                    _context.SaveChanges();

                    // ACTUALIZAR ESTADO DE PEDIDOS DE SERVICIO
                    var pedidosInvolucrados = detalles
                        .Where(d => d.TablaReferencia == "DPEDSERVICIO" && d.IdReferencia != null)
                        .Select(d => _context.DPedidosServicio.Where(dp => dp.Id == d.IdReferencia).Select(dp => dp.PedidoServicioId).FirstOrDefault())
                        .Distinct().ToList();

                    var estadoAtendidoTotal = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "PED");
                    var estadoAtendidoParcial = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "PED");

                    foreach (var pedId in pedidosInvolucrados)
                    {
                        var pedido = _context.PedServicios.Find(pedId);
                        bool todoAtendido = !_context.DPedidosServicio
                            .Any(dp => dp.PedidoServicioId == pedId && dp.CantidadAtendida < dp.Cantidad);

                        if (todoAtendido) pedido.EstadoId = estadoAtendidoTotal.Id;
                        else pedido.EstadoId = estadoAtendidoParcial.Id;
                    }

                    orden.EstadoId = estadoAprobado.Id;
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = "OS Aprobada y saldos actualizados." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult RechazarOrden(int id)
        {
            try
            {
                var orden = _context.OrdenServicios.Find(id);
                // CORRECCIÓN: Usar ANULADO
                var estadoAnulado = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "ORDEN");

                if (orden != null && estadoAnulado != null)
                {
                    orden.EstadoId = estadoAnulado.Id;
                    _context.SaveChanges();
                    return Json(new { status = true, message = "OS Anulada." });
                }
                return Json(new { status = false, message = "Error al anular." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
    }
}