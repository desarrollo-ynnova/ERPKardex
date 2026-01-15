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

        #region 1. LISTADO DE ÓRDENES POR PAGAR
        // Este método cambia ligeramente: Ahora debe listar las APROBADAS que NO estén "Pagado Total"
        // Es decir, incluye las "Pendientes" y las "Pagado Parcial".

        [HttpGet]
        public async Task<JsonResult> GetOrdenesPorPagar()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                // 1. Obtener IDs clave
                var estAprobado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Aprobado" && e.Tabla == "ORDEN");
                var estPagadoTotal = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Pagado Total" && e.Tabla == "FINANZAS");

                int idAprobado = estAprobado?.Id ?? 0;
                int idPagadoTotal = estPagadoTotal?.Id ?? 0;

                // 2. Compras: Aprobadas Y (EstadoPago ES NULL O EstadoPago != PagadoTotal)
                var compras = from o in _context.OrdenCompras
                              join ent in _context.Entidades on o.EntidadId equals ent.Id
                              join mon in _context.Monedas on o.MonedaId equals mon.Id
                              join est in _context.Estados on o.EstadoPagoId equals est.Id
                              where o.EstadoId == idAprobado
                                 && (o.EstadoPagoId == null || o.EstadoPagoId != idPagadoTotal)
                              select new
                              {
                                  Id = o.Id,
                                  Tipo = "COMPRA",
                                  Numero = o.Numero,
                                  Fecha = o.FechaEmision,
                                  Proveedor = ent.RazonSocial,
                                  Moneda = mon.Nombre,
                                  MonedaSimbolo = mon.Simbolo,
                                  EstadoPago = est.Nombre,
                                  Total = o.Total,
                                  EmpresaId = o.EmpresaId
                              };

                // 3. Servicios
                var servicios = from o in _context.OrdenServicios
                                join ent in _context.Entidades on o.EntidadId equals ent.Id
                                join mon in _context.Monedas on o.MonedaId equals mon.Id
                                join est in _context.Estados on o.EstadoPagoId equals est.Id
                                where o.EstadoId == idAprobado
                                   && (o.EstadoPagoId == null || o.EstadoPagoId != idPagadoTotal)
                                select new
                                {
                                    Id = o.Id,
                                    Tipo = "SERVICIO",
                                    Numero = o.Numero,
                                    Fecha = o.FechaEmision,
                                    Proveedor = ent.RazonSocial,
                                    Moneda = mon.Nombre,
                                    MonedaSimbolo = mon.Simbolo,
                                    EstadoPago = est.Nombre,
                                    Total = o.Total,
                                    EmpresaId = o.EmpresaId
                                };

                var union = await compras.Union(servicios).OrderByDescending(x => x.Fecha).ToListAsync();

                if (!esGlobal) union = union.Where(x => x.EmpresaId == miEmpresaId).ToList();

                // Calcular saldo pendiente para mostrarlo en la vista (Opcional pero útil)
                // Para simplificar la vista actual, mandamos el Total Original, 
                // pero podrías mandar 'SaldoPendiente' haciendo una subconsulta aquí.

                var data = union.Select(x => new
                {
                    x.Id,
                    x.Tipo,
                    x.Numero,
                    Fecha = x.Fecha.GetValueOrDefault().ToString("dd/MM/yyyy"),
                    x.Proveedor,
                    x.Moneda,
                    x.MonedaSimbolo,
                    Total = x.Total,
                    x.EstadoPago,
                });

                return Json(new { status = true, data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 2. DATOS PARA EL FORMULARIO (Sin cambios mayores)
        [HttpGet]
        public async Task<JsonResult> GetDatosOrden(int id, string tipo)
        {
            try
            {
                string condicionPago = "";
                string monedaNombre = "";
                string monedaSimbolo = "";
                decimal total = 0;
                DateTime fechaEmision = DateTime.Now;
                int monedaId = 0;
                decimal pagadoHastaAhora = 0; // Nuevo: Saber cuánto ya se pagó

                if (tipo == "COMPRA")
                {
                    var oc = await _context.OrdenCompras.FindAsync(id);
                    if (oc != null)
                    {
                        condicionPago = oc.CondicionPago;
                        total = oc.Total ?? 0;
                        fechaEmision = oc.FechaEmision ?? DateTime.Now;
                        monedaId = oc.MonedaId ?? 0;

                        // Monedas
                        var monedaOc = _context.Monedas.FirstOrDefault(mo => mo.Id == monedaId);
                        monedaNombre = monedaOc?.Nombre ?? "";
                        monedaSimbolo = monedaOc?.Simbolo ?? "";

                        // Sumar pagos previos
                        pagadoHastaAhora = await _context.OrdenPagos
                            .Where(p => p.OrdenCompraId == id && p.EstadoId == 1) // 1=Generado/Valido
                            .SumAsync(p => p.MontoAbonado);
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

                        // Monedas
                        var monedaOs = _context.Monedas.FirstOrDefault(mo => mo.Id == monedaId);
                        monedaNombre = monedaOs?.Nombre ?? "";
                        monedaSimbolo = monedaOs?.Simbolo ?? "";

                        pagadoHastaAhora = await _context.OrdenPagos
                            .Where(p => p.OrdenServicioId == id && p.EstadoId == 1)
                            .SumAsync(p => p.MontoAbonado);
                    }
                }

                var bancos = await _context.Bancos.Where(b => b.Estado == true).Select(b => new { b.Id, b.Nombre, b.Ruc }).ToListAsync();

                // Calculamos el saldo sugerido a pagar
                decimal saldoPendiente = total - pagadoHastaAhora;
                if (saldoPendiente < 0) saldoPendiente = 0;

                return Json(new
                {
                    status = true,
                    data = new
                    {
                        FechaOrden = fechaEmision.ToString("yyyy-MM-dd"),
                        Condicion = condicionPago,
                        Total = total,
                        MonedaId = monedaId,
                        MonedaNombre = monedaNombre,
                        MonedaSimbolo = monedaSimbolo,
                        PagadoPrevio = pagadoHastaAhora, // Para mostrar info
                        SaldoPendiente = saldoPendiente  // Para sugerir en el input
                    },
                    bancos
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region 3. REGISTRAR PAGO (LÓGICA PARCIALES)

        [HttpPost]
        public async Task<JsonResult> RegistrarPago(string pagoJson, DateTime fechaPagoReal, IFormFile archivoVoucher)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var pago = JsonConvert.DeserializeObject<OrdenPago>(pagoJson);

                    // --- ARCHIVO ---
                    if (archivoVoucher != null && archivoVoucher.Length > 0)
                    {
                        string webRootPath = _env.WebRootPath;
                        string uploadDir = Path.Combine(webRootPath, "uploads", "vouchers");
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                        string extension = Path.GetExtension(archivoVoucher.FileName);
                        string nombreArchivo = $"VOUCHER_{DateTime.Now:yyyyMMddHHmmss}_{new Random().Next(1000, 9999)}{extension}";
                        string rutaCompleta = Path.Combine(uploadDir, nombreArchivo);

                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await archivoVoucher.CopyToAsync(stream);
                        }
                        pago.RutaVoucher = $"uploads/vouchers/{nombreArchivo}";
                    }

                    // --- T.C. ---
                    var tcDia = await _context.TipoCambios
                        .Where(x => x.Fecha.Date == fechaPagoReal.Date)
                        .Select(x => x.TcVenta).FirstOrDefaultAsync();

                    if (tcDia <= 0) return Json(new { status = false, message = $"No existe T.C. para {fechaPagoReal:dd/MM/yyyy}." });

                    pago.TipoCambioPago = tcDia;
                    pago.FechaPago = fechaPagoReal;
                    pago.FechaRegistro = DateTime.Now;
                    pago.UsuarioRegistroId = UsuarioActualId;
                    pago.EstadoId = 1; // Generado

                    // --- CÁLCULO DÍAS RETRASO ---
                    if (pago.FechaOrden.HasValue)
                    {
                        var fechaVencimiento = pago.FechaOrden.Value.AddDays(pago.DiasCredito);
                        var diferencia = (fechaPagoReal - fechaVencimiento).TotalDays;
                        pago.DiasRetraso = diferencia > 0 ? (int)diferencia : 0;
                    }

                    // --- LÓGICA DE ESTADOS FINANCIEROS (PARCIAL vs TOTAL) ---

                    // 1. Obtener IDs de estados FINANCIEROS
                    var estPagadoTotal = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Pagado Total" && e.Tabla == "FINANZAS");
                    var estPagadoParcial = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Pagado Parcial" && e.Tabla == "FINANZAS");

                    if (estPagadoTotal == null || estPagadoParcial == null)
                        throw new Exception("Faltan configurar estados financieros (Pagado Total/Parcial) en la tabla 'estado'.");

                    decimal totalOrden = 0;
                    decimal pagadoAcumulado = 0; // Incluyendo el actual

                    // 2. Procesar según tipo
                    if (pago.OrdenCompraId.HasValue)
                    {
                        var oc = await _context.OrdenCompras.FindAsync(pago.OrdenCompraId);
                        if (oc != null)
                        {
                            totalOrden = oc.Total ?? 0;
                            // Sumar lo histórico + lo actual
                            var previos = await _context.OrdenPagos
                                .Where(p => p.OrdenCompraId == pago.OrdenCompraId && p.EstadoId == 1)
                                .SumAsync(p => p.MontoAbonado);

                            pagadoAcumulado = previos + pago.MontoAbonado;

                            // Decisión
                            if (pagadoAcumulado >= totalOrden - 0.1m) // Tolerancia de 10 céntimos por redondeo
                            {
                                oc.EstadoPagoId = estPagadoTotal.Id;
                                // Opcional: También poner el estado administrativo en 'Pagado' si deseas mantener compatibilidad visual antigua
                                // oc.EstadoId = (await _context.Estados.FirstAsync(e => e.Nombre == "Pagado" && e.Tabla == "ORDEN")).Id; 
                            }
                            else
                            {
                                oc.EstadoPagoId = estPagadoParcial.Id;
                            }
                            _context.OrdenCompras.Update(oc);
                        }
                    }
                    else if (pago.OrdenServicioId.HasValue)
                    {
                        var os = await _context.OrdenServicios.FindAsync(pago.OrdenServicioId);
                        if (os != null)
                        {
                            totalOrden = os.Total ?? 0;
                            var previos = await _context.OrdenPagos
                                .Where(p => p.OrdenServicioId == pago.OrdenServicioId && p.EstadoId == 1)
                                .SumAsync(p => p.MontoAbonado);

                            pagadoAcumulado = previos + pago.MontoAbonado;

                            if (pagadoAcumulado >= totalOrden - 0.1m)
                            {
                                os.EstadoPagoId = estPagadoTotal.Id;
                            }
                            else
                            {
                                os.EstadoPagoId = estPagadoParcial.Id;
                            }
                            _context.OrdenServicios.Update(os);
                        }
                    }

                    // --- GUARDAR ---
                    _context.OrdenPagos.Add(pago);
                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    string msgExtra = (pagadoAcumulado < totalOrden) ? " (Pago Parcial registrado)" : " (Orden Cancelada Totalmente)";
                    return Json(new { status = true, message = "Pago registrado correctamente." + msgExtra });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = ex.Message });
                }
            }
        }

        #endregion

        #region 4. HISTORIAL (Sin cambios)
        [HttpGet]
        public async Task<JsonResult> GetHistorialPagos()
        {
            try
            {
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
                                       MonedaSimbolo = m.Simbolo,
                                       Monto = op.MontoAbonado,
                                       Banco = b.Nombre,
                                       Operacion = op.NumeroOperacion,
                                       TC = op.TipoCambioPago,
                                       Estado = (op.EstadoId == 1) ? "GENERADO" : "ANULADO",
                                       op.RutaVoucher,
                                       DiasCredito = op.DiasCredito,
                                       DiasRetraso = op.DiasRetraso,
                                       Condicion = op.CondicionPago
                                   }).ToListAsync();

                return Json(new { status = true, data = pagos });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion
    }
}