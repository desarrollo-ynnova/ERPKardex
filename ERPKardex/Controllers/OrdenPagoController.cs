using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class OrdenPagoController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public OrdenPagoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index() => View();

        #region 1. LISTADO DE ÓRDENES POR PAGAR (APROBADAS)

        [HttpGet]
        public async Task<JsonResult> GetOrdenesPorPagar()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                // 1. Obtener ID del estado "Aprobado" para Órdenes
                var estadoAprobado = await _context.Estados
                    .Where(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN")
                    .Select(e => e.Id)
                    .FirstOrDefaultAsync();

                // 2. Buscar Órdenes de Compra Aprobadas
                var compras = from o in _context.OrdenCompras
                              join ent in _context.Entidades on o.EntidadId equals ent.Id
                              join mon in _context.Monedas on o.MonedaId equals mon.Id
                              where o.EstadoId == estadoAprobado
                              select new
                              {
                                  Id = o.Id,
                                  Tipo = "COMPRA",
                                  Numero = o.Numero,
                                  Fecha = o.FechaEmision,
                                  Proveedor = ent.RazonSocial,
                                  Moneda = mon.Nombre,
                                  Total = o.Total,
                                  EmpresaId = o.EmpresaId
                              };

                // 3. Buscar Órdenes de Servicio Aprobadas
                var servicios = from o in _context.OrdenServicios
                                join ent in _context.Entidades on o.EntidadId equals ent.Id
                                join mon in _context.Monedas on o.MonedaId equals mon.Id
                                where o.EstadoId == estadoAprobado
                                select new
                                {
                                    Id = o.Id,
                                    Tipo = "SERVICIO",
                                    Numero = o.Numero,
                                    Fecha = o.FechaEmision,
                                    Proveedor = ent.RazonSocial,
                                    Moneda = mon.Nombre,
                                    Total = o.Total,
                                    EmpresaId = o.EmpresaId
                                };

                // 4. Unir y Filtrar
                var union = await compras.Union(servicios)
                                         .OrderByDescending(x => x.Fecha)
                                         .ToListAsync();

                if (!esGlobal)
                {
                    union = union.Where(x => x.EmpresaId == miEmpresaId).ToList();
                }

                // Formatear para la vista
                var data = union.Select(x => new
                {
                    x.Id,
                    x.Tipo,
                    x.Numero,
                    Fecha = x.Fecha.Value.ToString("dd/MM/yyyy"),
                    x.Proveedor,
                    x.Moneda,
                    Total = x.Total
                });

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 2. DATOS PARA EL FORMULARIO DE PAGO

        [HttpGet]
        public async Task<JsonResult> GetDatosOrden(int id, string tipo)
        {
            try
            {
                dynamic orden = null;
                string condicionPago = "";
                decimal total = 0;
                DateTime fechaEmision = DateTime.Now;
                int monedaId = 0;

                if (tipo == "COMPRA")
                {
                    var oc = await _context.OrdenCompras.FindAsync(id);
                    if (oc != null)
                    {
                        condicionPago = oc.CondicionPago;
                        total = oc.Total ?? 0;
                        fechaEmision = oc.FechaEmision ?? DateTime.Now;
                        monedaId = oc.MonedaId ?? 0;
                    }
                }
                else
                {
                    var os = await _context.OrdenServicios.FindAsync(id);
                    if (os != null)
                    {
                        condicionPago = os.CondicionPago;
                        total = os.Total ?? 0;
                        fechaEmision = os.FechaEmision ?? DateTime.Now;
                        monedaId = os.MonedaId ?? 0;
                    }
                }

                // Bancos Disponibles
                var bancos = await _context.Bancos.Where(b => b.Estado == true).Select(b => new { b.Id, b.Nombre, b.Ruc }).ToListAsync();

                return Json(new
                {
                    status = true,
                    data = new
                    {
                        FechaOrden = fechaEmision.ToString("yyyy-MM-dd"),
                        Condicion = condicionPago,
                        Total = total,
                        MonedaId = monedaId
                    },
                    bancos
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region 3. REGISTRAR PAGO

        [HttpPost]
        public async Task<JsonResult> RegistrarPago(string pagoJson, DateTime fechaPagoReal, IFormFile archivoVoucher)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. DESERIALIZAR EL JSON (Convertir string a Objeto C#)
                    var pago = JsonConvert.DeserializeObject<OrdenPago>(pagoJson);

                    // 2. GESTIÓN DEL ARCHIVO (Si enviaron uno)
                    if (archivoVoucher != null && archivoVoucher.Length > 0)
                    {
                        // Ruta: wwwroot/uploads/vouchers
                        string webRootPath = _env.WebRootPath;
                        string uploadDir = Path.Combine(webRootPath, "uploads", "vouchers");

                        // Crear carpeta si no existe
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                        // Nombre único: VOUCHER_20231025_999.pdf
                        string extension = Path.GetExtension(archivoVoucher.FileName);
                        string nombreArchivo = $"VOUCHER_{DateTime.Now:yyyyMMddHHmmss}_{new Random().Next(1000, 9999)}{extension}";
                        string rutaCompleta = Path.Combine(uploadDir, nombreArchivo);

                        // Guardar en disco
                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await archivoVoucher.CopyToAsync(stream);
                        }

                        // Guardar ruta relativa en BD
                        pago.RutaVoucher = $"uploads/vouchers/{nombreArchivo}";
                    }

                    // 3. VALIDACIÓN T.C.
                    var tcDia = await _context.TipoCambios
                                              .Where(x => x.Fecha.Date == fechaPagoReal.Date)
                                              .Select(x => x.TcVenta)
                                              .FirstOrDefaultAsync();

                    if (tcDia <= 0) return Json(new { status = false, message = $"No existe T.C. para {fechaPagoReal:dd/MM/yyyy}." });

                    pago.TipoCambioPago = tcDia;
                    pago.FechaPago = fechaPagoReal;
                    pago.FechaRegistro = DateTime.Now;
                    pago.UsuarioRegistroId = UsuarioActualId;
                    pago.EstadoId = 1; // Generado

                    // 4. CÁLCULO RETRASO
                    if (pago.FechaOrden.HasValue)
                    {
                        var fechaVencimiento = pago.FechaOrden.Value.AddDays(pago.DiasCredito);
                        var diferencia = (fechaPagoReal - fechaVencimiento).TotalDays;
                        pago.DiasRetraso = diferencia > 0 ? (int)diferencia : 0;
                    }

                    // 5. CAMBIAR ESTADO ORDEN ORIGINAL A "PAGADO"
                    var estadoPagado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Pagado" && e.Tabla == "ORDEN");
                    if (estadoPagado == null) throw new Exception("Estado 'Pagado' no configurado.");

                    if (pago.OrdenCompraId.HasValue)
                    {
                        var oc = await _context.OrdenCompras.FindAsync(pago.OrdenCompraId);
                        if (oc != null) { oc.EstadoId = estadoPagado.Id; _context.OrdenCompras.Update(oc); }
                    }
                    else if (pago.OrdenServicioId.HasValue)
                    {
                        var os = await _context.OrdenServicios.FindAsync(pago.OrdenServicioId);
                        if (os != null) { os.EstadoId = estadoPagado.Id; _context.OrdenServicios.Update(os); }
                    }

                    // 6. GUARDAR
                    _context.OrdenPagos.Add(pago);
                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return Json(new { status = true, message = "Pago registrado y archivo subido correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        #endregion
        #region 4. HISTORIAL DE PAGOS REALIZADOS

        [HttpGet]
        public async Task<JsonResult> GetHistorialPagos()
        {
            try
            {
                // Join con la nueva estructura de Banco y Monedas
                var pagos = await (from op in _context.OrdenPagos
                                   join b in _context.Bancos on op.BancoId equals b.Id
                                   join m in _context.Monedas on op.MonedaOrdenId equals m.Id
                                   orderby op.FechaPago descending
                                   select new
                                   {
                                       op.Id,
                                       Fecha = op.FechaPago.ToString("dd/MM/yyyy"),
                                       Orden = op.NumeroOrden,
                                       Moneda = m.Nombre,
                                       Monto = op.MontoAbonado,
                                       Banco = b.Nombre,
                                       Operacion = op.NumeroOperacion,
                                       TC = op.TipoCambioPago,
                                       Estado = (op.EstadoId == 1) ? "GENERADO" : "ANULADO",
                                       op.RutaVoucher,

                                       // NUEVOS DATOS PARA VISUALIZACIÓN
                                       DiasCredito = op.DiasCredito,
                                       DiasRetraso = op.DiasRetraso,
                                       Condicion = op.CondicionPago // Para mostrar si fue Contado o Crédito
                                   }).ToListAsync();

                return Json(new { status = true, data = pagos });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

    }
}