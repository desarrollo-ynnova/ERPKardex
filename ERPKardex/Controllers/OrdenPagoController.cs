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

        #region 1. LISTADO DE DEUDAS PENDIENTES (SEMÁFORO DE VENCIMIENTO)
        [HttpGet]
        public async Task<JsonResult> GetDeudasPendientes()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;

                // Buscamos documentos con SALDO > 0 y estado activo (No anulados)
                // Incluimos Facturas, Boletas, RH y Anticipos
                var data = await (from d in _context.DocumentosPagar
                                  join t in _context.TiposDocumentoInterno on d.TipoDocumentoInternoId equals t.Id
                                  join prov in _context.Proveedores on d.ProveedorId equals prov.Id
                                  join mon in _context.Monedas on d.MonedaId equals mon.Id
                                  join est in _context.Estados on d.EstadoId equals est.Id
                                  where d.EmpresaId == miEmpresaId
                                     && d.Saldo > 0 // <--- CLAVE: Solo lo que se debe
                                     && est.Nombre != "Anulado"
                                  orderby d.FechaVencimiento ascending // Lo más urgente primero
                                  select new
                                  {
                                      d.Id,
                                      Tipo = t.Codigo, // FAC, BOL, ANT
                                      Numero = d.Serie + "-" + d.Numero,
                                      Proveedor = prov.RazonSocial,
                                      FechaEmision = d.FechaEmision,
                                      FechaVencimiento = d.FechaVencimiento, // <--- CRÍTICO
                                      Moneda = mon.Simbolo,
                                      Total = d.Total,
                                      Saldo = d.Saldo
                                  }).ToListAsync();

                // Procesamiento en memoria para cálculo de días
                var hoy = DateTime.Now.Date;
                var resultado = data.Select(x =>
                {
                    double diasRestantes = 0;
                    string estadoVenc = "VIGENTE";
                    string colorSem = "badge-success";

                    if (x.FechaVencimiento.HasValue)
                    {
                        diasRestantes = (x.FechaVencimiento.Value.Date - hoy).TotalDays;

                        if (diasRestantes < 0)
                        {
                            estadoVenc = "VENCIDO";
                            colorSem = "badge-danger";
                        }
                        else if (diasRestantes <= 3)
                        {
                            estadoVenc = "POR VENCER";
                            colorSem = "badge-warning";
                        }
                    }
                    else
                    {
                        // Si es anticipo, no vence, es inmediato
                        if (x.Tipo == "ANT") { estadoVenc = "INMEDIATO"; colorSem = "badge-info"; }
                    }

                    return new
                    {
                        x.Id,
                        x.Tipo,
                        x.Numero,
                        x.Proveedor,
                        Emision = x.FechaEmision.Value.ToString("dd/MM/yyyy"),
                        Vencimiento = x.FechaVencimiento.HasValue ? x.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "-",
                        x.Moneda,
                        Total = x.Total,
                        Saldo = x.Saldo,
                        EstadoVencimiento = estadoVenc,
                        ColorSem = colorSem,
                        Dias = Math.Abs(diasRestantes)
                    };
                }).ToList();

                return Json(new { status = true, data = resultado });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 2. DATOS PARA EL MODAL (DETALLE DEUDA)
        [HttpGet]
        public async Task<JsonResult> GetDatosDeuda(int id)
        {
            try
            {
                var consulta = await (from d in _context.DocumentosPagar
                                      join p in _context.Proveedores on d.ProveedorId equals p.Id
                                      join m in _context.Monedas on d.MonedaId equals m.Id
                                      where d.Id == id
                                      select new
                                      {
                                          DocumentoId = d.Id,
                                          Serie = d.Serie,
                                          Numero = d.Numero,
                                          Total = d.Total,
                                          Saldo = d.Saldo,
                                          FechaVencimiento = d.FechaVencimiento,
                                          MonedaId = d.MonedaId,
                                          MonedaSimbolo = m.Simbolo,
                                          // Datos del Proveedor para Tesorería
                                          ProveedorNombre = p.RazonSocial,
                                          CuentaDetraccion = p.NumeroCuentaDetracciones,
                                          // Cuentas Bancarias
                                          M1 = p.MonedaIdUno,
                                          C1 = p.NumeroCuentaUno,
                                          CCI1 = p.NumeroCciUno,
                                          M2 = p.MonedaIdDos,
                                          C2 = p.NumeroCuentaDos,
                                          CCI2 = p.NumeroCciDos,
                                          M3 = p.MonedaIdTres,
                                          C3 = p.NumeroCuentaTres,
                                          CCI3 = p.NumeroCciTres
                                      }).FirstOrDefaultAsync();

                if (consulta == null) throw new Exception("No se encontró el documento o proveedor.");

                // Bancos para el combo del modal
                var bancos = await _context.Bancos.Where(b => b.Estado == true).Select(b => new { b.Id, b.Nombre }).ToListAsync();
                var tipoOrdenPagos = await _context.TipoOrdenPagos.Where(t => t.Estado.Value == true).Select(t => new { t.Id, t.Nombre }).ToListAsync();

                // Filtramos las cuentas que coinciden con la moneda del documento
                var cuentasFiltradas = new List<object>();
                if (consulta.M1 == consulta.MonedaId && !string.IsNullOrEmpty(consulta.C1))
                    cuentasFiltradas.Add(new { Banco = "CTA 1", Cuenta = consulta.C1, CCI = consulta.CCI1 });

                if (consulta.M2 == consulta.MonedaId && !string.IsNullOrEmpty(consulta.C2))
                    cuentasFiltradas.Add(new { Banco = "CTA 2", Cuenta = consulta.C2, CCI = consulta.CCI2 });

                if (consulta.M3 == consulta.MonedaId && !string.IsNullOrEmpty(consulta.C3))
                    cuentasFiltradas.Add(new { Banco = "CTA 3", Cuenta = consulta.C3, CCI = consulta.CCI3 });

                return Json(new
                {
                    status = true,
                    data = new
                    {
                        Documento = $"{consulta.Serie}-{consulta.Numero}",
                        ProveedorNombre = consulta.ProveedorNombre,
                        FechaVencimiento = consulta.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "-",
                        Total = consulta.Total,
                        Saldo = consulta.Saldo,
                        MonedaId = consulta.MonedaId,
                        MonedaSimbolo = consulta.MonedaSimbolo,
                        CuentasProveedor = cuentasFiltradas,
                        CuentaDetraccion = consulta.CuentaDetraccion
                    },
                    bancos,
                    tipoOrdenPagos,
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 3. REGISTRAR PAGO (AMORTIZACIÓN)
        [HttpPost]
        public async Task<JsonResult> RegistrarPago(string pagoJson, DateTime fechaPagoReal, IFormFile archivoVoucher)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var pago = JsonConvert.DeserializeObject<OrdenPago>(pagoJson);
                    var estadoPagado = _context.Estados.FirstOrDefault(e => e.Tabla == "ORDEN_PAGO" && e.Nombre == "Pagado");

                    // 1. SUBIDA DE ARCHIVO
                    if (archivoVoucher != null && archivoVoucher.Length > 0)
                    {
                        string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "vouchers");
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                        string fileName = $"OP_{DateTime.Now:yyyyMMddHHmmss}_{new Random().Next(100, 999)}{Path.GetExtension(archivoVoucher.FileName)}";
                        using (var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create)) { await archivoVoucher.CopyToAsync(stream); }
                        pago.RutaVoucher = $"uploads/vouchers/{fileName}";
                    }

                    // 2. VALIDACIÓN DE CAJA / TIPO CAMBIO
                    var tcDia = await _context.TipoCambios.Where(x => x.Fecha.Date == fechaPagoReal.Date).Select(x => x.TcVenta).FirstOrDefaultAsync();
                    if (tcDia <= 0) return Json(new { status = false, message = $"No hay Tipo de Cambio para {fechaPagoReal:dd/MM/yyyy}." });

                    // 3. DATOS GENERALES
                    pago.TipoCambio = tcDia;
                    pago.FechaPago = fechaPagoReal;
                    pago.FechaRegistro = DateTime.Now;
                    pago.UsuarioRegistroId = UsuarioActualId;
                    pago.EmpresaId = EmpresaUsuarioId;
                    pago.EstadoId = estadoPagado?.Id; // Pagado

                    // Generar Correlativo OP-00001
                    var ultimo = _context.OrdenPagos.Where(x => x.EmpresaId == EmpresaUsuarioId).OrderByDescending(x => x.Numero).FirstOrDefault();
                    int n = 1;
                    if (ultimo != null && int.TryParse(ultimo.Numero.Replace("OP-", ""), out int num)) n = num + 1;
                    pago.Numero = "OP-" + n.ToString("D5");

                    _context.OrdenPagos.Add(pago);
                    await _context.SaveChangesAsync(); // Guardamos para tener ID si fuera necesario

                    // 4. ACTUALIZACIÓN DE LA DEUDA (PROVISIÓN)
                    var doc = await _context.DocumentosPagar.FindAsync(pago.DocumentoPagarId);

                    if (pago.MontoPagado > doc.Saldo + 0.1m) // Pequeña tolerancia
                        throw new Exception($"El pago ({pago.MontoPagado}) excede el saldo pendiente ({doc.Saldo}).");

                    // RESTA DEL SALDO
                    doc.Saldo -= pago.MontoPagado.GetValueOrDefault();

                    // CAMBIO DE ESTADO
                    var estCancelado = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Cancelado");
                    var estDisponible = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Disponible");
                    var estPorPagar = _context.Estados.FirstOrDefault(x => x.Tabla == "DOCUMENTO_PAGAR" && x.Nombre == "Por Pagar");

                    if (doc.Saldo <= 0)
                    {
                        var esAnticipo = _context.TiposDocumentoInterno.Where(tdi => tdi.Id == doc.TipoDocumentoInternoId).Any(tdi => tdi.Codigo == "ANT");
                        if (esAnticipo)
                        {
                            doc.EstadoId = estDisponible?.Id;
                        }
                        else
                        {
                            doc.EstadoId = estCancelado?.Id;
                        }
                    }
                    else
                    {
                        doc.EstadoId = estPorPagar?.Id;
                    }
                    // 5. REGISTRO EN HISTORIAL DE APLICACIONES (Para que salga en el historial de la factura)
                    // Creamos un registro virtual de aplicación para trazabilidad visual
                    _context.DocumentoPagarAplicaciones.Add(new DocumentoPagarAplicacion
                    {
                        EmpresaId = EmpresaUsuarioId,
                        DocumentoCargoId = doc.Id,
                        // DocumentoAbonoId = NULL, // No hay documento abono, es un pago directo
                        MontoAplicado = pago.MontoPagado.GetValueOrDefault(),
                        FechaAplicacion = DateTime.Now,
                        UsuarioId = UsuarioActualId
                    });

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    string estadoFinal = doc.Saldo <= 0 ? "DEUDA CANCELADA TOTALMENTE" : $"AMORTIZACIÓN REGISTRADA. SALDO: {doc.Saldo}";
                    return Json(new { status = true, message = $"Operación Exitosa. {estadoFinal}" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }
        #endregion

        #region 4. HISTORIAL DE PAGOS
        [HttpGet]
        public async Task<JsonResult> GetHistorialPagos()
        {
            try
            {
                // Listamos las Órdenes de Pago realizadas
                var data = await (from op in _context.OrdenPagos
                                  join top in _context.TipoOrdenPagos on op.TipoOrdenPagoId equals top.Id
                                  join doc in _context.DocumentosPagar on op.DocumentoPagarId equals doc.Id
                                  join prov in _context.Proveedores on doc.ProveedorId equals prov.Id
                                  join t in _context.TiposDocumentoInterno on doc.TipoDocumentoInternoId equals t.Id
                                  join mon in _context.Monedas on op.MonedaId equals mon.Id
                                  join ban in _context.Bancos on op.BancoId equals ban.Id into banG
                                  from b in banG.DefaultIfEmpty()
                                  where op.EmpresaId == EmpresaUsuarioId
                                  orderby op.FechaPago descending
                                  select new
                                  {
                                      op.Id,
                                      Fecha = op.FechaPago.ToString("dd/MM/yyyy"),
                                      TipoOrdenPago = top.Nombre,
                                      NumeroOP = op.Numero,
                                      DocRef = t.Codigo + " " + doc.Serie + "-" + doc.Numero,
                                      Proveedor = prov.RazonSocial,
                                      Moneda = mon.Simbolo,
                                      Monto = op.MontoPagado,
                                      Banco = b != null ? b.Nombre : "CAJA/EFECTIVO",
                                      Operacion = op.NumeroOperacion,
                                      op.RutaVoucher
                                  }).ToListAsync();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion
    }
}