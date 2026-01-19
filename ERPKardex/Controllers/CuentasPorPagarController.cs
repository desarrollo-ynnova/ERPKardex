using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class CuentasPorPagarController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CuentasPorPagarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // 0. VISTAS (Tus interfaces separadas)
        // =========================================================================
        public IActionResult Index() => View();
        public IActionResult RegistroAnticipo() => View();
        public IActionResult RegistroProvision() => View(); // Facturas, Boletas, RH
        public IActionResult RegistroNotaCredito() => View();
        public IActionResult RegistroNotaDebito() => View();

        // =========================================================================
        // 1. UTILITARIOS COMPARTIDOS (Para llenar combos y datos)
        // =========================================================================

        [HttpGet]
        public JsonResult GetDatosOrden(int ordenId, string tipoOrigen) // 'OC' o 'OS'
        {
            try
            {
                // Buscamos la data para "pintar" el formulario automáticamente
                if (tipoOrigen == "OC")
                {
                    var orden = _context.OrdenCompras
                        .Where(x => x.Id == ordenId)
                        .Select(x => new
                        {
                            x.ProveedorId,
                            Proveedor = _context.Proveedores.Where(p => p.Id == x.ProveedorId).Select(p => p.RazonSocial).FirstOrDefault(),
                            x.MonedaId,
                            x.TipoCambio,
                            x.Total // Total de la orden para referencia
                        }).FirstOrDefault();
                    return Json(new { status = true, data = orden });
                }
                else
                {
                    var orden = _context.OrdenServicios
                        .Where(x => x.Id == ordenId)
                        .Select(x => new
                        {
                            x.ProveedorId,
                            Proveedor = _context.Proveedores.Where(p => p.Id == x.ProveedorId).Select(p => p.RazonSocial).FirstOrDefault(),
                            x.MonedaId,
                            x.TipoCambio,
                            x.Total
                        }).FirstOrDefault();
                    return Json(new { status = true, data = orden });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult BuscarFacturasProveedor(int proveedorId)
        {
            try
            {
                // 1. Tipos válidos (Facturas, Boletas, RH)
                var tiposValidos = _context.TiposDocumentoInterno
                    .Where(t => t.Codigo == "FAC" || t.Codigo == "BOL" || t.Codigo == "RH")
                    .Select(t => t.Id).ToList();

                // 2. Consulta: Solo documentos activos con Saldo pendiente (Opcional: Si quieres permitir NC a facturas pagadas, quita x.Saldo > 0)
                // Usualmente la NC se aplica a lo que sea, pero aquí mostramos datos útiles.
                var facturas = _context.DocumentosPagar
                    .Where(x => x.ProveedorId == proveedorId
                             && tiposValidos.Contains(x.TipoDocumentoInternoId)
                             && x.EstadoId == 1) // Activo
                    .OrderByDescending(x => x.FechaEmision)
                    .Select(x => new
                    {
                        x.Id,
                        x.Serie,
                        x.Numero,
                        Fecha = x.FechaEmision.Value.ToString("dd/MM/yyyy"),
                        x.Total,
                        x.Saldo,
                        x.MonedaId
                    }).ToList();

                return Json(new { status = true, data = facturas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // =========================================================================
        // 2. LÓGICA DE ANTICIPOS (Formulario 1)
        // =========================================================================
        [HttpPost]
        public JsonResult GuardarAnticipo(DocumentoPagar doc, string tipoOrigenOrden) // tipoOrigenOrden: 'OC' o 'OS'
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Configuración Inicial
                    PrepararDocumentoBase(doc);

                    // 2. Obtener ID del Tipo 'ANT'
                    var tipoAnt = _context.TiposDocumentoInterno.FirstOrDefault(x => x.Codigo == "ANT");
                    if (tipoAnt == null) throw new Exception("El tipo de documento ANTICIPO (ANT) no está configurado en BD.");
                    doc.TipoDocumentoInternoId = tipoAnt.Id;

                    // 3. Validar Amarre a Orden (Obligatorio para Anticipo)
                    ValidarExistenciaOrden(doc, tipoOrigenOrden);

                    // 4. Lógica de Negocio Anticipo:
                    // - Nace con saldo a favor (Saldo = Total)
                    // - NO tiene detalle de ítems (d_documento_pagar vacío)
                    doc.Saldo = doc.Total;
                    doc.DocumentoReferenciaId = null; // No nace de una factura, nace de una orden

                    _context.DocumentosPagar.Add(doc);
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { status = true, message = "Anticipo registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        // =========================================================================
        // 3. LÓGICA DE PROVISIONES - FACTURAS/BOLETAS (Formulario 2)
        // =========================================================================
        [HttpPost]
        public JsonResult GuardarProvision(DocumentoPagar doc, string codigoTipoDoc, string tipoOrigenOrden)
        {
            // codigoTipoDoc: 'FAC', 'BOL', 'RH'
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    PrepararDocumentoBase(doc);

                    // 1. Validar Tipo
                    var tipo = _context.TiposDocumentoInterno.FirstOrDefault(x => x.Codigo == codigoTipoDoc);
                    if (tipo == null) throw new Exception($"El tipo {codigoTipoDoc} no existe.");
                    doc.TipoDocumentoInternoId = tipo.Id;

                    // 2. Validar Orden
                    ValidarExistenciaOrden(doc, tipoOrigenOrden);

                    // 3. Guardar Cabecera
                    doc.Saldo = doc.Total; // La deuda total es el saldo inicial
                    _context.DocumentosPagar.Add(doc);
                    _context.SaveChanges(); // Necesitamos el ID para el detalle

                    // 4. LA CLAVE: JALAR ÍTEMS (SNAPSHOT)
                    // Aquí se cumple tu requerimiento: "Jalar tal cual sin modificar"
                    if (tipoOrigenOrden == "OC" && doc.OrdenCompraId.HasValue)
                    {
                        var items = _context.DOrdenCompras.Where(x => x.OrdenCompraId == doc.OrdenCompraId).ToList();
                        foreach (var item in items)
                        {
                            _context.DDocumentosPagar.Add(new DDocumentoPagar
                            {
                                DocumentoPagarId = doc.Id,
                                IdReferencia = item.Id,
                                TablaReferencia = "DORDENCOMPRA", // Trazabilidad
                                ProductoId = item.ProductoId,
                                Descripcion = item.Descripcion,
                                UnidadMedida = item.UnidadMedida,
                                Cantidad = item.Cantidad,         // Cantidad original de la orden
                                PrecioUnitario = item.PrecioUnitario, // Precio pactado
                                Total = item.Total
                            });
                        }
                    }
                    else if (tipoOrigenOrden == "OS" && doc.OrdenServicioId.HasValue)
                    {
                        var items = _context.DOrdenServicios.Where(x => x.OrdenServicioId == doc.OrdenServicioId).ToList();
                        foreach (var item in items)
                        {
                            _context.DDocumentosPagar.Add(new DDocumentoPagar
                            {
                                DocumentoPagarId = doc.Id,
                                IdReferencia = item.Id,
                                TablaReferencia = "DORDENSERVICIO",
                                ProductoId = item.ProductoId,
                                Descripcion = item.Descripcion,
                                UnidadMedida = item.UnidadMedida,
                                Cantidad = item.Cantidad,
                                PrecioUnitario = item.PrecioUnitario,
                                Total = item.Total
                            });
                        }
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                    return Json(new { status = true, message = "Documento provisionado y detalle copiado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        // =========================================================================
        // 4. LÓGICA DE NOTAS DE CRÉDITO / DÉBITO (Formulario 3 y 4)
        // =========================================================================
        [HttpPost]
        public JsonResult GuardarNotaCreditoDebito(DocumentoPagar doc, string codigoTipoDoc)
        {
            // codigoTipoDoc: 'NC' o 'ND'
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    PrepararDocumentoBase(doc);

                    var tipo = _context.TiposDocumentoInterno.FirstOrDefault(x => x.Codigo == codigoTipoDoc);
                    if (tipo == null) throw new Exception("Tipo de nota no configurado.");
                    doc.TipoDocumentoInternoId = tipo.Id;

                    // 1. VALIDACIÓN ESPECÍFICA: DEBE TENER FACTURA PADRE
                    if (doc.DocumentoReferenciaId == null || doc.DocumentoReferenciaId == 0)
                        throw new Exception("La Nota de Crédito/Débito debe estar amarrada a una Factura obligatoriamente.");

                    // Verificar que la factura padre exista (Simulación FK)
                    var facturaPadre = _context.DocumentosPagar.Find(doc.DocumentoReferenciaId);
                    if (facturaPadre == null) throw new Exception("La factura de referencia no existe.");

                    // 2. HERENCIA DE DATOS (Opcional, pero recomendado para consistencia)
                    // Una NC suele heredar la Orden de la Factura Padre para reportes
                    doc.OrdenCompraId = facturaPadre.OrdenCompraId;
                    doc.OrdenServicioId = facturaPadre.OrdenServicioId;

                    // 3. REGISTRO
                    // La NC nace con saldo = Total. 
                    // Luego en el módulo "Aplicación" se cruza: Factura vs NC.
                    doc.Saldo = doc.Total;

                    _context.DocumentosPagar.Add(doc);
                    _context.SaveChanges();

                    // NOTA: Si quisieras registrar detalle de items devueltos en la NC,
                    // se haría aquí insertando en ddocumento_pagar manualmente.
                    // Por ahora, según tu flujo, registramos el valor financiero.

                    transaction.Commit();
                    return Json(new { status = true, message = "Nota registrada correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        // =========================================================================
        // 5. MÉTODOS PRIVADOS (HELPER) - PARA EVITAR CÓDIGO REPETIDO
        // =========================================================================

        private void PrepararDocumentoBase(DocumentoPagar doc)
        {
            doc.EmpresaId = EmpresaUsuarioId; // BaseController
            doc.UsuarioRegistroId = UsuarioActualId; // BaseController
            doc.FechaRegistro = DateTime.Now;
            doc.EstadoId = 1; // Activo/Registrado

            // Seguridad: Si viene nulo el saldo, asumimos total
            if (doc.Saldo == 0 && doc.Total > 0) doc.Saldo = doc.Total;
        }

        private void ValidarExistenciaOrden(DocumentoPagar doc, string tipoOrigen)
        {
            if (tipoOrigen == "OC")
            {
                if (doc.OrdenCompraId == null) throw new Exception("Debe seleccionar una Orden de Compra.");
                var existe = _context.OrdenCompras.Any(x => x.Id == doc.OrdenCompraId);
                if (!existe) throw new Exception("La Orden de Compra indicada no existe.");
                doc.OrdenServicioId = null; // Limpiar por si acaso
            }
            else if (tipoOrigen == "OS")
            {
                if (doc.OrdenServicioId == null) throw new Exception("Debe seleccionar una Orden de Servicio.");
                var existe = _context.OrdenServicios.Any(x => x.Id == doc.OrdenServicioId);
                if (!existe) throw new Exception("La Orden de Servicio indicada no existe.");
                doc.OrdenCompraId = null;
            }
        }
        [HttpGet]
        public JsonResult BuscarOrdenesPendientes(string tipoOrigen) // 'OC' o 'OS'
        {
            try
            {
                // Regla de Negocio: Solo mostrar órdenes APROBADAS (que ya son oficiales)
                // y que no estén Anuladas.

                var lista = new List<object>();

                if (tipoOrigen == "OC")
                {
                    // Filtramos Estado Aprobado (ajusta el ID o nombre según tu tabla Estado)
                    // Asumo que 'Aprobado' existe en tu tabla Estado para ORDEN
                    var query = from o in _context.OrdenCompras
                                join p in _context.Proveedores on o.ProveedorId equals p.Id
                                join m in _context.Monedas on o.MonedaId equals m.Id
                                join e in _context.Estados on o.EstadoId equals e.Id
                                where e.Nombre == "Aprobado" // O "Generado" si permites anticipos antes de aprobar
                                orderby o.FechaEmision descending
                                select new
                                {
                                    o.Id,
                                    o.Numero,
                                    Proveedor = p.RazonSocial,
                                    p.Ruc,
                                    o.ProveedorId,
                                    o.MonedaId,
                                    MonedaNombre = m.Simbolo,
                                    Fecha = o.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                    o.Total
                                };

                    return Json(new { status = true, data = query.ToList() });
                }
                else
                {
                    // Lógica para Orden Servicio
                    var query = from o in _context.OrdenServicios
                                join p in _context.Proveedores on o.ProveedorId equals p.Id
                                join m in _context.Monedas on o.MonedaId equals m.Id
                                join e in _context.Estados on o.EstadoId equals e.Id
                                where e.Nombre == "Aprobado"
                                orderby o.FechaEmision descending
                                select new
                                {
                                    o.Id,
                                    o.Numero,
                                    Proveedor = p.RazonSocial,
                                    p.Ruc,
                                    o.ProveedorId,
                                    o.MonedaId,
                                    MonedaNombre = m.Simbolo,
                                    Fecha = o.FechaEmision.GetValueOrDefault().ToString("dd/MM/yyyy"),
                                    o.Total
                                };

                    return Json(new { status = true, data = query.ToList() });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetDetallesOrden(int ordenId, string tipoOrigen) // 'OC' o 'OS'
        {
            try
            {
                if (tipoOrigen == "OC")
                {
                    var detalles = _context.DOrdenCompras
                        .Where(x => x.OrdenCompraId == ordenId)
                        .Select(x => new
                        {
                            x.Item,
                            Producto = x.Descripcion, // O join con Producto si prefieres el nombre del maestro
                            x.UnidadMedida,
                            x.Cantidad,
                            x.PrecioUnitario,
                            x.Total
                        }).ToList();
                    return Json(new { status = true, data = detalles });
                }
                else
                {
                    var detalles = _context.DOrdenServicios
                        .Where(x => x.OrdenServicioId == ordenId)
                        .Select(x => new
                        {
                            x.Item,
                            Producto = x.Descripcion,
                            x.UnidadMedida,
                            x.Cantidad,
                            x.PrecioUnitario,
                            x.Total
                        }).ToList();
                    return Json(new { status = true, data = detalles });
                }
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
    }
}