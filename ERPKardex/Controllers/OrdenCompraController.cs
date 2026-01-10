using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class OrdenCompraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdenCompraController(ApplicationDbContext context)
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

                var query = from o in _context.OrdenCompras
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
                                Moneda = mon.Nombre,
                                Total = o.Total,
                                Estado = est.Nombre,
                                o.EstadoId,
                                // Estos campos sirven para validaciones en el front
                                o.Observacion
                            };

                return Json(new { status = true, data = query.ToList() });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // DETALLE PARA EL "OJITO" (MODAL)
        [HttpGet]
        public JsonResult GetDetalleOrden(int id)
        {
            try
            {
                var detalles = (from d in _context.DOrdenCompras
                                join p in _context.Productos on d.ProductoId equals p.Id
                                where d.OrdenCompraId == id
                                select new
                                {
                                    d.Item,
                                    Producto = d.Descripcion, // Usamos la descripción snapshot
                                    d.UnidadMedida,
                                    d.Cantidad,
                                    d.PrecioUnitario,
                                    d.Total,
                                    // Referencia para saber de qué pedido vino
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
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                // Entidades (Proveedores)
                var proveedores = _context.Entidades.Where(x => x.Estado == true && (empresaId == 4 || x.EmpresaId == empresaId))
                    .Select(x => new { x.Id, x.Ruc, x.RazonSocial }).ToList();

                // Monedas
                var monedas = _context.Monedas.Where(x => x.Estado == true).ToList();

                return Json(new { status = true, proveedores, monedas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. LÓGICA DE JALAR PEDIDOS (EL CEREBRO)

        // A. LISTAR PEDIDOS CON SALDO PENDIENTE
        [HttpGet]
        public JsonResult GetPedidosPendientes(int? empresaFiltroId)
        {
            try
            {
                var empresaUsuario = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                int idEmpresa = (empresaUsuario == 4) ? (empresaFiltroId ?? 0) : empresaUsuario;

                // Buscamos pedidos Generados o Aprobados (según tu flujo, usualmente 'Generado' en Pedido ya es listo para OCO)
                var estadosValidos = _context.Estados.Where(e => (e.Nombre == "Generado" || e.Nombre == "Aprobado") && e.Tabla == "PED")
                                                     .Select(e => e.Id).ToList();

                // Filtramos cabeceras que tengan al menos un detalle con saldo
                var pedidos = (from p in _context.PedCompras
                               join d in _context.DPedidoCompras on p.Id equals d.PedidoCompraId
                               where p.EmpresaId == idEmpresa
                                     && estadosValidos.Contains(p.EstadoId ?? 0)
                                     && d.CantidadSolicitada > (d.CantidadAtendida ?? 0) // Filtro básico de BD
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

        // B. OBTENER CABECERA DE UN PEDIDO (PARA HEREDAR SUCURSAL/ALMACÉN)
        [HttpGet]
        public JsonResult GetCabeceraPedido(int pedidoId)
        {
            try
            {
                var pedido = _context.PedCompras.Where(p => p.Id == pedidoId)
                    .Select(p => new
                    {
                        p.SucursalId,
                        p.AlmacenId,
                        p.LugarDestino,
                        // Traemos nombres para mostrar en inputs readonly
                        SucursalNombre = _context.Sucursales.Where(s => s.Id == p.SucursalId).Select(s => s.Nombre).FirstOrDefault(),
                        AlmacenNombre = _context.Almacenes.Where(a => a.Id == p.AlmacenId).Select(a => a.Nombre).FirstOrDefault()
                    }).FirstOrDefault();

                return Json(new { status = true, data = pedido });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // C. OBTENER DETALLES DEL PEDIDO (CON CÁLCULO DE SALDO "EN VUELO")
        [HttpGet]
        public JsonResult GetDetallesPedidoParaOrden(int pedidoId)
        {
            try
            {
                // 1. Obtener los ítems del Pedido
                var itemsPedido = (from d in _context.DPedidoCompras
                                   join cc in _context.CentroCostos on d.CentroCostoId equals cc.Id into joinCC
                                   from cc in joinCC.DefaultIfEmpty()
                                   where d.PedidoCompraId == pedidoId
                                   select new
                                   {
                                       d.Id, // ID DPedCompra (Referencia)
                                       d.ProductoId,
                                       d.DescripcionLibre,
                                       d.UnidadMedida,
                                       d.CantidadSolicitada,
                                       d.CantidadAtendida, // Lo que ya está APROBADO en otras OCOs
                                       d.CentroCostoId,
                                       CentroCostoNombre = cc != null ? cc.Codigo : "-",
                                       d.Item
                                   }).ToList();

                // 2. Obtener lo que está "Comprometido" (En OCOs Generadas/Emitidas pero NO Aprobadas ni Anuladas)
                // Esto evita el problema de las "4 órdenes iguales".
                var estadoGeneradoOS = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN")?.Id ?? 0;

                var comprometidos = (from doc in _context.DOrdenCompras // O DOrdenServicios según el controller
                                     join oc in _context.OrdenCompras on doc.OrdenCompraId equals oc.Id
                                     where oc.EstadoId == estadoGeneradoOS // <--- USAR EL ID DE GENERADO
                                        && doc.TablaReferencia == "DPEDCOMPRA"
                                     select new { doc.IdReferencia, doc.Cantidad })
                                     .ToList();

                // 3. Cruzar información en memoria
                var resultado = itemsPedido.Select(ip =>
                {
                    // Sumar todo lo que está en órdenes "Emitidas" para este item específico
                    decimal cantComprometida = comprometidos.Where(x => x.IdReferencia == ip.Id).Sum(x => x.Cantidad ?? 0);

                    // Saldo Real = Solicitado - (Atendido Real + Comprometido en Borradores)
                    decimal saldoDisponible = (ip.CantidadSolicitada ?? 0) - ((ip.CantidadAtendida ?? 0) + cantComprometida);

                    return new
                    {
                        ip.Id, // IdReferencia
                        ip.ProductoId,
                        ip.Item,
                        Descripcion = ip.DescripcionLibre,
                        ip.UnidadMedida,
                        ip.CentroCostoId,
                        ip.CentroCostoNombre,
                        CantidadPedido = ip.CantidadSolicitada,
                        SaldoDisponible = saldoDisponible > 0 ? saldoDisponible : 0 // Si es negativo, 0
                    };
                })
                .Where(x => x.SaldoDisponible > 0) // Solo devolvemos lo que tiene saldo
                .ToList();

                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 4. GUARDAR Y APROBAR

        [HttpPost]
        public JsonResult Guardar(OrdenCompra cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // --- VALIDACIONES INICIALES ---
                    var empresaUsuario = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    // Validar Estado Inicial
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");
                    var tipoDoc = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == "OCO"); // O "OS"

                    if (estadoGenerado == null || tipoDoc == null)
                        throw new Exception("Falta configuración de estados (Generado) o documentos.");

                    // Correlativo
                    var ultimo = _context.OrdenCompras
                        .Where(x => x.EmpresaId == cabecera.EmpresaId && x.TipoDocumentoInternoId == tipoDoc.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero).FirstOrDefault();

                    int nro = 1;
                    if (!string.IsNullOrEmpty(ultimo))
                    {
                        var partes = ultimo.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int val)) nro = val + 1;
                    }

                    // --- GUARDAR CABECERA ---
                    cabecera.Numero = $"OCO-{nro.ToString("D10")}";
                    cabecera.TipoDocumentoInternoId = tipoDoc.Id;
                    cabecera.UsuarioCreacionId = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.EstadoId = estadoGenerado.Id;

                    if (cabecera.FechaEmision == DateTime.MinValue) cabecera.FechaEmision = DateTime.Now;

                    _context.OrdenCompras.Add(cabecera);
                    _context.SaveChanges();

                    // --- GUARDAR DETALLES Y VALIDAR SALDOS ---
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var items = JsonConvert.DeserializeObject<List<DOrdenCompra>>(detallesJson);
                        int itemCounter = 1;

                        foreach (var det in items)
                        {
                            // Validación de seguridad backend: ¿Me estoy pasando del saldo?
                            if (det.IdReferencia != null && det.TablaReferencia == "DPEDCOMPRA")
                            {
                                // Recalculamos saldo rápido para evitar hackeos
                                var pedItem = _context.DPedidoCompras.AsNoTracking().FirstOrDefault(x => x.Id == det.IdReferencia);
                                if (pedItem != null)
                                {
                                    // Ojo: Aquí solo valido contra lo 'Atendido' físico real para no bloquear, 
                                    // pero idealmente se usa la lógica de "GetDetallesPedidoParaOrden".
                                    // Como es 'Guardar' (Borrador), permitimos guardar, la restricción fuerte es al Aprobar.
                                    // PERO, para cumplir tu requerimiento de las 4 ordenes, validamos aquí también.

                                    // Sumar lo que hay en otras OCOs emitidas
                                    var emitidosOtros = _context.DOrdenCompras
                                        .Where(x => x.IdReferencia == det.IdReferencia && x.TablaReferencia == "DPEDCOMPRA"
                                                 && x.OrdenCompraId != cabecera.Id // Excluir la actual si fuera edición
                                                 && _context.OrdenCompras.Any(o => o.Id == x.OrdenCompraId && o.EstadoId == estadoGenerado.Id))
                                        .Sum(x => x.Cantidad ?? 0);

                                    decimal saldoReal = (pedItem.CantidadSolicitada ?? 0) - (pedItem.CantidadAtendida ?? 0) - emitidosOtros;

                                    if ((det.Cantidad ?? 0) > saldoReal)
                                    {
                                        throw new Exception($"El producto {det.Descripcion} excede el saldo disponible ({saldoReal}). Revise si hay otras OCOs generadas.");
                                    }
                                }
                            }

                            det.Id = 0;
                            det.OrdenCompraId = cabecera.Id;
                            det.EmpresaId = cabecera.EmpresaId;
                            det.Item = itemCounter.ToString("D3");

                            // Cálculos monetarios básicos (Backend trust)
                            // Si es inafecto (ej. Importación), el impuesto es 0
                            if (cabecera.IncluyeIgv == false)
                            {
                                det.Impuesto = 0;
                                det.ValorVenta = det.Total; // O la lógica inversa según tu front
                            }

                            _context.DOrdenCompras.Add(det);
                            itemCounter++;
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = $"Orden {cabecera.Numero} generada correctamente." });
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
                    // 1. Obtener la Orden y sus detalles
                    var orden = _context.OrdenCompras.Find(id);
                    if (orden == null) throw new Exception("Orden no encontrada.");

                    var estadoAprobado = _context.Estados.FirstOrDefault(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");
                    // CAMBIO AQUÍ: Validar contra "Generado"
                    var estadoGenerado = _context.Estados.FirstOrDefault(e => e.Nombre == "Generado" && e.Tabla == "ORDEN");

                    // Validación: Solo aprobar si está Generado
                    if (orden.EstadoId != estadoGenerado.Id) throw new Exception("Solo se pueden aprobar órdenes en estado Generado.");

                    var detalles = _context.DOrdenCompras.Where(d => d.OrdenCompraId == id).ToList();

                    // 2. ACTUALIZAR SALDOS EN EL PEDIDO (EL MOMENTO DE LA VERDAD)
                    foreach (var det in detalles)
                    {
                        if (det.IdReferencia != null && det.TablaReferencia == "DPEDCOMPRA")
                        {
                            var pedItem = _context.DPedidoCompras.Find(det.IdReferencia);
                            if (pedItem != null)
                            {
                                // Check Final: ¿Aún hay saldo? (Por si alguien aprobó otra orden hace 1 milisegundo)
                                decimal saldoActual = (pedItem.CantidadSolicitada ?? 0) - (pedItem.CantidadAtendida ?? 0);
                                if ((det.Cantidad ?? 0) > saldoActual)
                                {
                                    throw new Exception($"Conflicto de saldo en item {det.Item}. Saldo actual: {saldoActual}.");
                                }

                                // Actualizar Saldo Atendido
                                pedItem.CantidadAtendida = (pedItem.CantidadAtendida ?? 0) + (det.Cantidad ?? 0);
                            }
                        }
                    }
                    _context.SaveChanges();

                    // 3. VERIFICAR SI LOS PEDIDOS ORIGEN SE COMPLETARON
                    // Obtenemos los IDs de los pedidos involucrados
                    var pedidosInvolucrados = detalles
                        .Where(d => d.TablaReferencia == "DPEDCOMPRA" && d.IdReferencia != null)
                        .Select(d => _context.DPedidoCompras.Where(dp => dp.Id == d.IdReferencia).Select(dp => dp.PedidoCompraId).FirstOrDefault())
                        .Distinct()
                        .ToList();

                    var estadoAtendidoTotal = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "PED");
                    var estadoAtendidoParcial = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "PED");

                    foreach (var pedId in pedidosInvolucrados)
                    {
                        var pedido = _context.PedCompras.Find(pedId);
                        // Verificar si TODOS los items de ese pedido están full atendidos
                        bool todoAtendido = !_context.DPedidoCompras
                            .Any(dp => dp.PedidoCompraId == pedId && dp.CantidadAtendida < dp.CantidadSolicitada);

                        if (todoAtendido) pedido.EstadoId = estadoAtendidoTotal.Id;
                        else pedido.EstadoId = estadoAtendidoParcial.Id;
                    }

                    // 4. CAMBIAR ESTADO DE LA ORDEN
                    orden.EstadoId = estadoAprobado.Id;
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = "Orden Aprobada y saldos actualizados." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error al aprobar: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult RechazarOrden(int id)
        {
            // Rechazar/Anular es simple: Solo cambia el estado. 
            // Como no hemos tocado saldo 'Atendido', no hay que devolver nada.
            // Y al estar Anulada, el "GetDetallesPedidoParaOrden" dejará de contarla como comprometida.
            try
            {
                var orden = _context.OrdenCompras.Find(id);
                var estadoAnulado = _context.Estados.FirstOrDefault(e => e.Nombre == "Anulado" && e.Tabla == "ORDEN");

                if (orden != null && estadoAnulado != null)
                {
                    orden.EstadoId = estadoAnulado.Id;
                    _context.SaveChanges();
                    return Json(new { status = true, message = "Orden anulada." });
                }
                return Json(new { status = false, message = "No se pudo anular." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion
    }
}