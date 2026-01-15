using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // 1. HERENCIA DE BASECONTROLLER
    public class OrdenServicioController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public OrdenServicioController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        #endregion

        #region 1. LISTADO (INDEX)

        [HttpGet]
        public async Task<JsonResult> GetOrdenesData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId; // BaseController
                var esGlobal = EsAdminGlobal;       // BaseController

                var query = from o in _context.OrdenServicios
                            join ent in _context.Entidades on o.EntidadId equals ent.Id
                            join est in _context.Estados on o.EstadoId equals est.Id
                            join est2 in _context.Estados on o.EstadoPagoId equals est2.Id
                            join mon in _context.Monedas on o.MonedaId equals mon.Id
                            orderby o.Id descending
                            select new
                            {
                                o.Id,
                                o.EmpresaId,
                                o.Numero,
                                Fecha = o.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                Proveedor = ent.RazonSocial,
                                Ruc = ent.Ruc,
                                Moneda = mon.Nombre,
                                Total = o.Total,
                                Estado = est.Nombre,
                                EstadoPago = est2.Nombre,
                                o.EstadoId,
                                o.Observacion
                            };

                // Filtro Seguridad
                if (!esGlobal)
                {
                    query = query.Where(x => x.EmpresaId == miEmpresaId);
                }

                var listado = await query.ToListAsync();
                return Json(new { status = true, data = listado });
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
                                    Producto = d.Descripcion,
                                    d.Lugar,
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

        #region 2. DATOS PARA REGISTRO (MAESTROS)

        [HttpGet]
        public JsonResult GetCombosRegistro()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                var proveedores = _context.Entidades
                    .Where(x => x.Estado == true && (esGlobal || x.EmpresaId == miEmpresaId))
                    .Select(x => new { x.Id, x.Ruc, x.RazonSocial }).ToList();

                var monedas = _context.Monedas.Where(x => x.Estado == true).ToList();

                return Json(new { status = true, proveedores, monedas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. LÓGICA DE JALAR PEDIDOS (CEREBRO)

        [HttpGet]
        public JsonResult GetPedidosPendientes(int? empresaFiltroId)
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;
                int idEmpresa = esGlobal ? (empresaFiltroId ?? 0) : miEmpresaId;

                var estadosValidos = _context.Estados
                    .Where(e => (e.Nombre == "Generado" || e.Nombre == "Atendido Parcial") && e.Tabla == "PED")
                    .Select(e => e.Id).ToList();

                var pedidos = (from p in _context.PedServicios
                               join d in _context.DPedidosServicio on p.Id equals d.PedidoServicioId
                               where p.EmpresaId == idEmpresa
                                     && estadosValidos.Contains(p.EstadoId ?? 0)
                                     && d.Cantidad > (d.CantidadAtendida ?? 0) // Filtro saldo
                               group p by new { p.Id, p.Numero, p.FechaEmision, p.FechaNecesaria, p.Observacion } into g
                               select new
                               {
                                   g.Key.Id,
                                   g.Key.Numero,
                                   Fecha = g.Key.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                   FechaNecesaria = g.Key.FechaNecesaria.GetValueOrDefault().ToString("dd/MM/yyyy"),
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
                // 1. Ítems del Pedido
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
                                       d.Lugar, // En servicio también hay lugar por detalle
                                       d.Item
                                   }).ToList();

                // 2. Comprometido en Borradores (Generado)
                var estadoGeneradoOS = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN")?.Id ?? 0;

                var comprometidos = (from doc in _context.DOrdenServicios
                                     join os in _context.OrdenServicios on doc.OrdenServicioId equals os.Id
                                     where os.EstadoId == estadoGeneradoOS
                                        && doc.TablaReferencia == "DPEDSERVICIO"
                                     select new { doc.IdReferencia, doc.Cantidad })
                                    .ToList();

                // 3. Cálculo final
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
                        ip.Lugar,
                        CantidadPedido = ip.CantidadSolicitada,
                        // Si saldo es negativo (sobre-atención previa), mostramos 0
                        SaldoDisponible = saldoDisponible > 0 ? saldoDisponible : 0
                    };
                })
                .Where(x => x.SaldoDisponible > 0) // Solo items con saldo
                .ToList();

                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 4. GUARDAR Y APROBAR

        [HttpPost]
        public JsonResult Guardar(OrdenServicio cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var usuarioId = UsuarioActualId; // BaseController

                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "OS");
                    var estPendientePago = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente Pago" && e.Tabla == "FINANZAS");


                    if (estadoGenerado == null || tipoDoc == null || estPendientePago == null) throw new Exception("Falta configuración (OS).");

                    // Correlativo
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

                    var tcDia = _context.TipoCambios
                                          .Where(x => x.Fecha.Date == cabecera.FechaEmision.GetValueOrDefault().Date)
                                          .Select(x => x.TcVenta)
                                          .FirstOrDefault();

                    if (tcDia <= 0) return Json(new { status = false, message = $"No existe Tipo de Cambio registrado para la fecha de pago {cabecera.FechaEmision.GetValueOrDefault().Date:dd/MM/yyyy}." });


                    // Cabecera
                    cabecera.Numero = $"OS-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioCreacionId = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.TipoCambio = tcDia;
                    cabecera.EstadoId = estadoGenerado.Id;
                    cabecera.EstadoPagoId = estPendientePago.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;

                    _context.OrdenServicios.Add(cabecera);
                    _context.SaveChanges();

                    // Detalles
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var items = JsonConvert.DeserializeObject<List<DOrdenServicio>>(detallesJson);
                        int itemCounter = 1;

                        foreach (var det in items)
                        {
                            // VALIDACIÓN DE SALDO COMENTADA (PERMITIR SOBRE-ATENCIÓN)
                            // La advertencia se maneja en el front.
                            /*
                            if (det.IdReferencia != null && det.TablaReferencia == "DPEDSERVICIO")
                            {
                                var pedItem = _context.DPedidosServicio.AsNoTracking().FirstOrDefault(x => x.Id == det.IdReferencia);
                                if (pedItem != null)
                                {
                                    var emitidosOtros = _context.DOrdenServicios
                                        .Where(x => x.IdReferencia == det.IdReferencia && x.TablaReferencia == "DPEDSERVICIO"
                                                 && x.OrdenServicioId != cabecera.Id
                                                 && _context.OrdenServicios.Any(o => o.Id == x.OrdenServicioId && o.EstadoId == estadoGenerado.Id))
                                        .Sum(x => x.Cantidad ?? 0);

                                    decimal saldoReal = (pedItem.Cantidad ?? 0) - (pedItem.CantidadAtendida ?? 0) - emitidosOtros;

                                    if ((det.Cantidad ?? 0) > saldoReal)
                                    {
                                        throw new Exception($"El servicio {det.Descripcion} excede saldo: {saldoReal}");
                                    }
                                }
                            }
                            */

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
                    return Json(new { status = false, message = ex.Message });
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
                    var usuarioId = UsuarioActualId; // BaseController
                    var orden = _context.OrdenServicios.Find(id);
                    if (orden == null) throw new Exception("Orden no encontrada.");

                    var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");

                    if (orden.EstadoId != estadoGenerado.Id) throw new Exception("Solo se pueden aprobar órdenes en estado Generado.");

                    // 1. CARGAMOS ESTADOS DE DETALLE PEDIDO (DPED)
                    var estDPedPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DPED")?.Id ?? 0;
                    var estDPedParcial = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "DPED")?.Id ?? 0;
                    var estDPedTotal = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "DPED")?.Id ?? 0;

                    var detalles = _context.DOrdenServicios.Where(d => d.OrdenServicioId == id).ToList();

                    // 2. ACTUALIZAR SALDOS Y ESTADOS DE ÍTEMS DE PEDIDO
                    foreach (var det in detalles)
                    {
                        if (det.IdReferencia != null && det.TablaReferencia == "DPEDSERVICIO")
                        {
                            var pedItem = _context.DPedidosServicio.Find(det.IdReferencia);
                            if (pedItem != null)
                            {
                                // A. Sumar cantidad atendida
                                pedItem.CantidadAtendida = (pedItem.CantidadAtendida ?? 0) + (det.Cantidad ?? 0);

                                // B. Determinar estado granular del ítem
                                if (pedItem.CantidadAtendida >= pedItem.Cantidad)
                                {
                                    pedItem.EstadoId = estDPedTotal;
                                }
                                else if (pedItem.CantidadAtendida > 0)
                                {
                                    pedItem.EstadoId = estDPedParcial;
                                }
                                else
                                {
                                    pedItem.EstadoId = estDPedPendiente;
                                }
                            }
                        }
                    }
                    _context.SaveChanges();

                    // 3. VERIFICAR ESTADO DE PEDIDOS (CABECERA)
                    var pedidosInvolucrados = detalles
                        .Where(d => d.TablaReferencia == "DPEDSERVICIO" && d.IdReferencia != null)
                        .Select(d => _context.DPedidosServicio.Where(dp => dp.Id == d.IdReferencia).Select(dp => dp.PedidoServicioId).FirstOrDefault())
                        .Distinct().ToList();

                    var estTotalPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "PED");
                    var estParcialPED = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "PED");

                    foreach (var pedId in pedidosInvolucrados)
                    {
                        var pedido = _context.PedServicios.Find(pedId);

                        // Lógica Cabecera: Si queda algo pendiente (Atendido < Solicitado) -> Parcial
                        bool hayPendientes = _context.DPedidosServicio
                            .Any(dp => dp.PedidoServicioId == pedId && (dp.CantidadAtendida ?? 0) < (dp.Cantidad ?? 0));

                        if (!hayPendientes) pedido.EstadoId = estTotalPED.Id;
                        else pedido.EstadoId = estParcialPED.Id;
                    }

                    // 4. APROBAR ORDEN
                    orden.EstadoId = estadoAprobado.Id;
                    orden.UsuarioAprobador = usuarioId;
                    orden.FechaAprobacion = DateTime.Now;
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = "Orden Aprobada." });
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
                var usuarioId = UsuarioActualId; // BaseController
                var orden = _context.OrdenServicios.Find(id);
                var estadoAnulado = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "ORDEN");

                if (orden != null && estadoAnulado != null)
                {
                    orden.EstadoId = estadoAnulado.Id;
                    orden.UsuarioAprobador = usuarioId;
                    orden.FechaAprobacion = DateTime.Now;
                    _context.SaveChanges();
                    return Json(new { status = true, message = "Orden anulada." });
                }
                return Json(new { status = false, message = "Error al anular." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
    }
}