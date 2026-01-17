using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class CuentaPagarController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CuentaPagarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();
        public IActionResult Registrar() => View();

        // =========================================================================
        // 1. CARGAR ÓRDENES ACTIVAS (Para el Combo "Orden Vinculada" - ANTICIPOS)
        // =========================================================================
        [HttpGet]
        public async Task<JsonResult> GetOrdenesActivas(int proveedorId)
        {
            try
            {
                // Buscamos órdenes Aprobadas (Estado 3) que aún no estén "Cerradas/Anuladas"
                // Esto sirve para seleccionar la orden en la cabecera sin jalar sus ítems
                var listado = await _context.OrdenCompras
                    .Where(x => x.ProveedorId == proveedorId
                             && x.EmpresaId == EmpresaUsuarioId
                             && x.EstadoId == 3) // 3 = Aprobado
                    .Select(x => new
                    {
                        Id = x.Id,
                        Texto = $"{x.Numero} - {x.FechaEmision:dd/MM/yyyy} (Total: {x.Total})"
                    })
                    .ToListAsync();

                return Json(new { status = true, data = listado });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // =========================================================================
        // 2. RECUPERAR DETALLES PENDIENTES (Para el Modal - FACTURAS REGULARES)
        // =========================================================================
        [HttpGet]
        public async Task<JsonResult> GetPendientesPorProveedor(int proveedorId)
        {
            try
            {
                var empresaId = EmpresaUsuarioId;

                // Buscar estado por nombre para no fallar por IDs
                var estadoAprobado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");
                if (estadoAprobado == null) return Json(new { status = false, message = "Falta estado 'Aprobado' en tabla ORDEN." });

                // A. BIENES
                var bienes = await (from d in _context.DOrdenCompras
                                    join c in _context.OrdenCompras on d.OrdenCompraId equals c.Id
                                    where c.ProveedorId == proveedorId
                                       && c.EmpresaId == empresaId
                                       && c.EstadoId == estadoAprobado.Id
                                       && d.Cantidad > (d.CantidadAtendida ?? 0)
                                    select new
                                    {
                                        Origen = "COMPRA",
                                        IdOrden = c.Id,
                                        NroOrden = c.Numero,
                                        Fecha = c.FechaEmision.Value.ToString("dd/MM/yyyy"),
                                        IdDetalle = d.Id,
                                        Producto = d.Descripcion,
                                        UM = d.UnidadMedida,
                                        Saldo = d.Cantidad - (d.CantidadAtendida ?? 0),
                                        Precio = d.PrecioUnitario,
                                        d.CentroCostoId
                                    }).ToListAsync();

                // B. SERVICIOS
                var servicios = await (from d in _context.DOrdenServicios
                                       join c in _context.OrdenServicios on d.OrdenServicioId equals c.Id
                                       where c.ProveedorId == proveedorId
                                          && c.EmpresaId == empresaId
                                          && c.EstadoId == estadoAprobado.Id
                                          && d.Cantidad > (d.CantidadAtendida ?? 0)
                                       select new
                                       {
                                           Origen = "SERVICIO",
                                           IdOrden = c.Id,
                                           NroOrden = c.Numero,
                                           Fecha = c.FechaEmision.Value.ToString("dd/MM/yyyy"),
                                           IdDetalle = d.Id,
                                           Producto = d.Descripcion,
                                           UM = d.UnidadMedida,
                                           Saldo = d.Cantidad - (d.CantidadAtendida ?? 0),
                                           Precio = d.PrecioUnitario,
                                           d.CentroCostoId
                                       }).ToListAsync();

                var listaFinal = bienes.Union(servicios).OrderBy(x => x.Fecha).ToList();
                return Json(new { status = true, data = listaFinal });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =========================================================================
        // 3. GUARDAR (PROCESO CENTRAL)
        // =========================================================================
        [HttpPost]
        public JsonResult Guardar(DocumentoPagar cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaId = EmpresaUsuarioId;
                    var usuarioId = UsuarioActualId;

                    // 1. OBTENER ESTADOS
                    var estDocRegistrado = _context.Estados.FirstOrDefault(e => e.Nombre == "Registrado" && e.Tabla == "CXP");
                    var estPagoPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "PAGO");
                    var estItemPendiente = _context.Estados.FirstOrDefault(e => e.Nombre == "Pendiente" && e.Tabla == "DORDEN");
                    var estItemParcial = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Parcial" && e.Tabla == "DORDEN");
                    var estItemTotal = _context.Estados.FirstOrDefault(e => e.Nombre == "Atendido Total" && e.Tabla == "DORDEN");

                    if (estDocRegistrado == null) throw new Exception("Error Config: Falta estado 'Registrado' en CXP");

                    // 2. CORRELATIVO INTERNO (FAC-0001)
                    var tipoDocSunat = _context.TipoDocumentos.Find(cabecera.TipoDocumentoId);
                    string codigoPrefijo = "DOC";
                    if (tipoDocSunat.Codigo == "01") codigoPrefijo = "FAC";
                    else if (tipoDocSunat.Codigo == "03") codigoPrefijo = "BOL";
                    else if (tipoDocSunat.Codigo == "07") codigoPrefijo = "NC";
                    else if (tipoDocSunat.Codigo == "08") codigoPrefijo = "ND";
                    else if (tipoDocSunat.Codigo == "02") codigoPrefijo = "RH";

                    var tipoDocInterno = _context.TiposDocumentoInterno.FirstOrDefault(t => t.Codigo == codigoPrefijo && t.Estado == true);
                    if (tipoDocInterno == null) throw new Exception($"Falta tipo interno '{codigoPrefijo}'.");

                    // Cálculo del número
                    var ultimoRegistro = _context.DocumentosPagar
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDocInterno.Id)
                        .OrderByDescending(x => x.CodigoInterno)
                        .Select(x => x.CodigoInterno)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;
                    if (!string.IsNullOrEmpty(ultimoRegistro))
                    {
                        var partes = ultimoRegistro.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int numeroActual))
                            nuevoCorrelativo = numeroActual + 1;
                    }
                    string codigoGenerado = $"{codigoPrefijo}-{nuevoCorrelativo.ToString("D10")}";

                    // 3. GUARDAR CABECERA
                    cabecera.EmpresaId = empresaId;
                    cabecera.UsuarioCreacionId = usuarioId;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.TipoDocumentoInternoId = tipoDocInterno.Id;
                    cabecera.CodigoInterno = codigoGenerado;
                    cabecera.EstadoId = estDocRegistrado.Id;
                    cabecera.EstadoPagoId = estPagoPendiente.Id;
                    cabecera.SaldoPendiente = cabecera.Total;

                    // Si es NC/ND, actualizar documento referencia
                    if (cabecera.DocReferenciaId != null && cabecera.DocReferenciaId > 0)
                    {
                        var docPadre = _context.DocumentosPagar.Find(cabecera.DocReferenciaId);
                        if (docPadre != null)
                        {
                            if (codigoPrefijo == "NC") docPadre.SaldoPendiente -= cabecera.Total;
                            else if (codigoPrefijo == "ND") docPadre.SaldoPendiente += cabecera.Total;

                            if (docPadre.SaldoPendiente < 0) docPadre.SaldoPendiente = 0;
                            _context.DocumentosPagar.Update(docPadre);
                        }
                    }

                    _context.DocumentosPagar.Add(cabecera);
                    _context.SaveChanges();

                    // 4. GUARDAR DETALLES
                    var detalles = JsonConvert.DeserializeObject<List<DDocumentoPagar>>(detallesJson);
                    int itemIdx = 1;
                    bool esNotaCredito = (codigoPrefijo == "NC");

                    foreach (var det in detalles)
                    {
                        det.Id = 0;
                        det.DocumentoPagarId = cabecera.Id;
                        det.Item = itemIdx;
                        _context.DDocumentosPagar.Add(det);

                        // --- LÓGICA DE ACTUALIZACIÓN DE SALDOS ---
                        // Solo entra si NO es manual (Tiene OrigenId y TablaOrigen)
                        if (det.OrigenId != null && det.OrigenId > 0 && !string.IsNullOrEmpty(det.TablaOrigen))
                        {
                            decimal cantidadOperacion = det.Cantidad;

                            if (det.TablaOrigen == "COMPRA")
                            {
                                var itemOrden = _context.DOrdenCompras.Find(det.OrigenId);
                                if (itemOrden != null)
                                {
                                    if (esNotaCredito) itemOrden.CantidadAtendida -= cantidadOperacion;
                                    else itemOrden.CantidadAtendida += cantidadOperacion;

                                    if (itemOrden.CantidadAtendida < 0) itemOrden.CantidadAtendida = 0;

                                    // Semáforo
                                    if (itemOrden.CantidadAtendida >= itemOrden.Cantidad) itemOrden.EstadoId = estItemTotal.Id;
                                    else if (itemOrden.CantidadAtendida > 0) itemOrden.EstadoId = estItemParcial.Id;
                                    else itemOrden.EstadoId = estItemPendiente.Id;

                                    _context.DOrdenCompras.Update(itemOrden);
                                }
                            }
                            else if (det.TablaOrigen == "SERVICIO")
                            {
                                var itemOrden = _context.DOrdenServicios.Find(det.OrigenId);
                                if (itemOrden != null)
                                {
                                    if (esNotaCredito) itemOrden.CantidadAtendida -= cantidadOperacion;
                                    else itemOrden.CantidadAtendida += cantidadOperacion;

                                    if (itemOrden.CantidadAtendida < 0) itemOrden.CantidadAtendida = 0;

                                    if (itemOrden.CantidadAtendida >= itemOrden.Cantidad) itemOrden.EstadoId = estItemTotal.Id;
                                    else if (itemOrden.CantidadAtendida > 0) itemOrden.EstadoId = estItemParcial.Id;
                                    else itemOrden.EstadoId = estItemPendiente.Id;

                                    _context.DOrdenServicios.Update(itemOrden);
                                }
                            }
                        }
                        itemIdx++;
                    }
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = $"Documento {codigoGenerado} registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return Json(new { status = false, message = "Error: " + msg });
                }
            }
        }

        // Combos Generales
        public JsonResult GetCombos()
        {
            var proveedores = _context.Proveedores.Where(x => x.Estado.Value && x.EmpresaId == EmpresaUsuarioId)
                                .Select(x => new { x.Id, x.RazonSocial, x.Ruc }).ToList();

            var tiposDoc = _context.TipoDocumentos.Where(x => x.Estado.Value)
                                .Select(x => new { x.Id, x.Descripcion, x.Codigo }).ToList();

            var monedas = _context.Monedas.Where(x => x.Estado.Value).ToList();

            return Json(new { proveedores, tiposDoc, monedas });
        }
    }
}