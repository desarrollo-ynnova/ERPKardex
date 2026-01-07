using ERPKardex.Data;
using ERPKardex.Models;
using ERPKardex.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class IngresoSalidaAlmController : Controller
    {
        private readonly ApplicationDbContext _context;
        public IngresoSalidaAlmController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region VISTAS
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        public IActionResult ReporteKardex() => View();
        public IActionResult ObtenerVistaRegistroEntidad()
        {
            // Retorna una vista parcial (_RegistroEntidad.cshtml) solo con los campos RUC y Razon Social
            return PartialView("_RegistroEntidad");
        }
        #endregion 

        #region APIs Maestro-Detalle
        [HttpGet]
        public JsonResult GetMovimientosData()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                var movimientos = (from isa in _context.IngresoSalidaAlms
                                   join mot in _context.Motivos on isa.MotivoId equals mot.Id
                                   join est in _context.Estados on isa.EstadoId equals est.Id
                                   join suc in _context.Sucursales on isa.SucursalId equals suc.Id
                                   join alm in _context.Almacenes on isa.AlmacenId equals alm.Id
                                   join emp in _context.Empresas on alm.EmpresaId equals emp.Id
                                   join tdi in _context.TiposDocumentoInterno on isa.TipoDocumentoInternoId equals tdi.Id
                                   join td in _context.TipoDocumentos on isa.TipoDocumentoId equals td.Id into joinDoc
                                   from tc in joinDoc.DefaultIfEmpty()
                                   join mon in _context.Monedas on isa.MonedaId equals mon.Id into joinMon
                                   from mo in joinMon.DefaultIfEmpty()
                                   where isa.EmpresaId == empresaId
                                   // where emp.Id == 1
                                   select new IngresoSalidaAlmViewModel
                                   {
                                       Id = isa.Id,
                                       Fecha = isa.Fecha,
                                       Numero = isa.Numero,
                                       MotivoId = isa.MotivoId,
                                       CodMotivo = mot.Codigo,
                                       TipoMovimiento = mot.TipoMovimiento,
                                       Motivo = mot.Descripcion,
                                       TipoDocumentoId = isa.TipoDocumentoId,
                                       TipoDocumento = tc.Descripcion,
                                       SerieDocumento = isa.SerieDocumento,
                                       NumeroDocumento = isa.NumeroDocumento,
                                       FechaDocumento = isa.FechaDocumento,
                                       MonedaId = isa.MonedaId,
                                       Moneda = mo.Nombre,
                                       EstadoId = isa.EstadoId,
                                       Estado = est.Nombre,
                                       SucursalId = isa.SucursalId,
                                       Sucursal = suc.Nombre,
                                       AlmacenId = isa.AlmacenId,
                                       Almacen = alm.Nombre,
                                       UsuarioId = isa.UsuarioId,
                                       FechaRegistro = isa.FechaRegistro,
                                   }).ToList();

                return Json(new { data = movimientos, message = "Movimientos retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, message = ex.Message, status = false });
            }
        }
        [HttpPost]
        [HttpPost]
        public JsonResult GuardarMovimiento(IngresoSalidaAlm cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                    int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;
                    int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                    // =================================================================================
                    // 1. DETERMINAR TIPO DE DOCUMENTO Y OBTENER SU ID
                    // =================================================================================

                    // Definimos el código según el tipo de movimiento (1: IALM, 0: SALM)
                    string codigoDoc = (cabecera.TipoMovimiento == true) ? "IALM" : "SALM";

                    // Buscamos el ID del tipo de documento en la maestra (Global)
                    var tipoDocInterno = _context.TiposDocumentoInterno
                        .FirstOrDefault(t => t.Codigo == codigoDoc && t.Estado == true);

                    if (tipoDocInterno == null)
                        throw new Exception($"No se encontró el tipo de documento interno '{codigoDoc}'.");

                    // =================================================================================
                    // 2. CALCULAR CORRELATIVO (Manual, por Empresa y Tipo)
                    // =================================================================================

                    // Buscamos el último movimiento de ESTA EMPRESA y de ESTE TIPO (IALM o SALM)
                    var ultimoRegistro = _context.IngresoSalidaAlms
                        .Where(x => x.EmpresaId == empresaId && x.TipoDocumentoInternoId == tipoDocInterno.Id)
                        .OrderByDescending(x => x.Numero)
                        .Select(x => x.Numero)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;

                    if (!string.IsNullOrEmpty(ultimoRegistro))
                    {
                        // El formato es "IALM-0000000005". Separamos por guion y tomamos la parte derecha.
                        var partes = ultimoRegistro.Split('-');
                        if (partes.Length > 1 && int.TryParse(partes[1], out int numeroActual))
                        {
                            nuevoCorrelativo = numeroActual + 1;
                        }
                    }

                    // Formateamos el nuevo número: IALM-0000000001
                    string numeroGenerado = $"{codigoDoc}-{nuevoCorrelativo.ToString("D10")}";

                    // Asignamos a la cabecera
                    cabecera.TipoDocumentoInternoId = tipoDocInterno.Id;
                    cabecera.Numero = numeroGenerado;
                    cabecera.FechaRegistro = DateTime.Now;
                    cabecera.UsuarioId = usuarioId;
                    cabecera.EmpresaId = empresaId;

                    // =================================================================================
                    // 3. GUARDAR CABECERA
                    // =================================================================================

                    _context.IngresoSalidaAlms.Add(cabecera);
                    _context.SaveChanges();

                    // =================================================================================
                    // 4. PROCESAR DETALLES Y STOCK (Igual que antes)
                    // =================================================================================

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DIngresoSalidaAlm>>(detallesJson);

                        foreach (var detalle in listaDetalles)
                        {
                            // Lógica de Stock (se mantiene intacta)
                            var registroStock = _context.StockAlmacenes
                                .FirstOrDefault(s => s.AlmacenId == cabecera.AlmacenId && s.ProductoId == detalle.ProductoId && s.EmpresaId == empresaId);

                            if (registroStock == null)
                            {
                                if (cabecera.TipoMovimiento == false)
                                    throw new Exception($"No existe registro de stock para el producto ID {detalle.ProductoId} en este almacén.");

                                registroStock = new StockAlmacen
                                {
                                    AlmacenId = cabecera.AlmacenId ?? 0,
                                    ProductoId = detalle.ProductoId ?? 0,
                                    StockActual = 0,
                                    UltimaActualizacion = DateTime.Now,
                                    EmpresaId = empresaId,
                                };
                                _context.StockAlmacenes.Add(registroStock);
                            }

                            if (cabecera.TipoMovimiento == true)
                            {
                                registroStock.StockActual += detalle.Cantidad ?? 0;
                            }
                            else
                            {
                                if ((registroStock.StockActual - (detalle.Cantidad ?? 0)) < 0)
                                {
                                    var codProd = _context.Productos.Where(p => p.Id == detalle.ProductoId).Select(p => p.Codigo).FirstOrDefault();
                                    throw new Exception($"Stock insuficiente para el producto {codProd}. Stock actual: {registroStock.StockActual}, Intento de salida: {detalle.Cantidad}");
                                }
                                registroStock.StockActual -= detalle.Cantidad ?? 0;
                            }
                            registroStock.UltimaActualizacion = DateTime.Now;

                            // Guardado del Detalle
                            detalle.Id = 0;
                            detalle.IngresoSalidaAlmId = cabecera.Id;
                            detalle.FechaRegistro = DateTime.Now;
                            detalle.EmpresaId = empresaId;

                            var prodData = _context.Productos
                                .Where(p => p.Id == detalle.ProductoId)
                                .Select(p => new { p.Codigo, p.DescripcionProducto, p.CodUnidadMedida })
                                .FirstOrDefault();

                            if (prodData != null)
                            {
                                detalle.CodProducto = prodData.Codigo;
                                detalle.DescripcionProducto = prodData.DescripcionProducto;
                                detalle.CodUnidadMedida = prodData.CodUnidadMedida;
                            }
                            _context.DIngresoSalidaAlms.Add(detalle);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { status = true, message = "Movimiento " + cabecera.Numero + " registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            }
        }
        #endregion

        #region APIs Combos Cabecera
        [HttpGet]
        public JsonResult GetEmpresas() => Json(new { data = _context.Empresas.ToList(), status = true });

        [HttpGet]
        public JsonResult GetSucursalesByEmpresa(int empresaId) =>
            Json(new { data = _context.Sucursales.Where(s => s.EmpresaId == empresaId && s.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetAlmacenesBySucursal(int sucursalId, int empresaId) =>
            Json(new { data = _context.Almacenes.Where(a => a.SucursalId == sucursalId && a.EmpresaId == empresaId && a.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMotivosData() => Json(new { data = _context.Motivos.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMonedaData() => Json(new { data = _context.Monedas.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetTipoDocumentoData() => Json(new { data = _context.TipoDocumentos.Where(t => t.Estado == true).ToList(), status = true });
        #endregion

        #region APIs Filtrado de Productos (Cascada)
        [HttpGet]
        public JsonResult GetProductos()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                return Json(new { data = _context.Productos.Where(p => p.EmpresaId == empresaId).Select(p => new { p.Id, p.Codigo, p.DescripcionProducto, p.CodUnidadMedida }).ToList(), status = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetStockProductoAlmacen(int? almacenId, int? productoId)
        {
            try
            {
                if (almacenId != null && productoId != null)
                {
                    var stockProducto = _context.StockAlmacenes.Where(sa => sa.AlmacenId == almacenId && sa.ProductoId == productoId).Select(sa => sa.StockActual).FirstOrDefault();
                    return Json(new { data = stockProducto, status = true, message = "Stock de almacén recuperado exitosamente." });
                }

                return Json(new ApiResponse { data = null, status = false, message = "Falta escoger almacén y/o producto." });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }

        }

        #endregion
        [HttpGet]
        public JsonResult GetCentrosCosto()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                var centrosCostosData = _context.CentroCostos.Where(c => c.Estado == true && c.EmpresaId == empresaId).ToList();
                return Json(new { data = centrosCostosData, status = true, message = "Centros de costo retornados exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetActividades()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                var actividadesData = _context.Actividades.Where(a => a.Estado == true && a.EmpresaId == empresaId).ToList();
                return Json(new { data = actividadesData, status = true, message = "Actividades retornadas exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetEntidades()
        {
            try
            {
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int empresaId = !string.IsNullOrEmpty(empresaIdClaim) ? int.Parse(empresaIdClaim) : 0;

                var entidadesData = _context.Entidades.Where(a => a.Estado == true).ToList();
                return Json(new { data = entidadesData, status = true, message = "Entidades retornadas exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { data = null, status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GenerarKardexValorizado(DateTime fechaInicio, DateTime fechaFin, int almacenId, int productoId, string metodo)
        {
            try
            {
                // 1. OBTENER METADATOS (AHORA CON JOIN DIRECTO)

                // Datos Empresa
                var datosEmpresa = (from a in _context.Almacenes
                                    join s in _context.Sucursales on a.SucursalId equals s.Id
                                    join e in _context.Empresas on s.EmpresaId equals e.Id
                                    where a.Id == almacenId
                                    select new { e.Ruc, e.RazonSocial, NombreSucursal = s.Nombre }).FirstOrDefault();

                // Datos Producto + Grupo + TIPO EXISTENCIA (La clave del éxito)
                var datosProducto = (from p in _context.Productos
                                     join g in _context.Grupos on p.GrupoId equals g.Id
                                     // Join con la nueva tabla
                                     join te in _context.TipoExistencias on g.TipoExistenciaId equals te.Id
                                     join u in _context.UnidadesMedida on p.CodUnidadMedida equals u.Codigo
                                     where p.Id == productoId
                                     select new
                                     {
                                         p.Codigo,
                                         p.DescripcionProducto,
                                         UnidadCodigo = u.Codigo,
                                         UnidadNombre = u.Descripcion,
                                         // Datos directos de BD, cero lógica hardcoded
                                         TipoExistenciaCod = te.Codigo,
                                         TipoExistenciaDesc = te.Nombre
                                     }).FirstOrDefault();

                // VALIDACIÓN DE SEGURIDAD
                // Si el grupo no tiene asignado un tipo de existencia, usamos un valor por defecto para no romper el reporte
                string codigoSunatTabla5 = datosProducto?.TipoExistenciaCod ?? "99";
                string nombreSunatTabla5 = datosProducto?.TipoExistenciaDesc ?? "OTROS (SIN CLASIFICAR)";

                // =================================================================================
                // 2. TU LÓGICA DE MOVIMIENTOS (INTACTA)
                // =================================================================================

                var movimientos = (from d in _context.DIngresoSalidaAlms
                                   join c in _context.IngresoSalidaAlms on d.IngresoSalidaAlmId equals c.Id
                                   join m in _context.Motivos on c.MotivoId equals m.Id
                                   join td in _context.TipoDocumentos on c.TipoDocumentoId equals td.Id into joinDoc
                                   from docRef in joinDoc.DefaultIfEmpty()
                                   where c.AlmacenId == almacenId && d.ProductoId == productoId
                                           && c.Fecha <= fechaFin && c.EstadoId == 1
                                   orderby c.Fecha, c.FechaRegistro
                                   select new
                                   {
                                       c.Fecha,
                                       // IMPORTANTE: Para SUNAT Tabla 10, necesitamos el CÓDIGO del tipo doc (ej: 01, 09), no solo la descripción
                                       CodigoTipoDoc = docRef != null ? docRef.Codigo : "00",
                                       TipoDoc = docRef != null ? docRef.Descripcion : "S/D",
                                       SerieDocumento = c.SerieDocumento != null ? c.SerieDocumento : "S/D",
                                       NumeroDocumento = c.NumeroDocumento != null ? c.NumeroDocumento : "S/D",
                                       // IMPORTANTE: Para SUNAT Tabla 12, necesitamos el CÓDIGO del motivo
                                       CodigoMotivo = m.Codigo,
                                       Motivo = m.Descripcion,
                                       m.TipoMovimiento,
                                       d.Cantidad,
                                       d.Precio,
                                       d.Total
                                   }).ToList();

                decimal saldoCant = 0;
                decimal saldoCostoTotal = 0;
                decimal costoPromedio = 0;
                var capasPeps = new List<(decimal Cantidad, decimal Precio)>();
                var reporte = new List<object>();

                // Variables para el Saldo Anterior
                decimal antCant = 0;
                decimal antCostoTotal = 0;

                foreach (var mov in movimientos)
                {
                    decimal cantMov = mov.Cantidad ?? 0;
                    decimal precioMov = mov.Precio ?? 0;
                    decimal cantEntrada = 0, costoUEntrada = 0, totalEntrada = 0;
                    decimal cantSalida = 0, costoUSalida = 0, totalSalida = 0;

                    if (mov.TipoMovimiento == true) // ENTRADA
                    {
                        cantEntrada = cantMov;
                        costoUEntrada = precioMov;
                        totalEntrada = cantEntrada * costoUEntrada;
                        saldoCant += cantEntrada;
                        saldoCostoTotal += totalEntrada;

                        if (metodo == "PEPS") capasPeps.Add((cantEntrada, costoUEntrada));
                        else costoPromedio = saldoCant > 0 ? saldoCostoTotal / saldoCant : 0;
                    }
                    else // SALIDA
                    {
                        cantSalida = cantMov;
                        if (metodo == "PEPS")
                        {
                            decimal cantPorRetirar = cantSalida;
                            decimal costoAcumuladoSalida = 0;
                            while (cantPorRetirar > 0 && capasPeps.Count > 0)
                            {
                                var capa = capasPeps[0];
                                if (capa.Cantidad <= cantPorRetirar)
                                {
                                    costoAcumuladoSalida += (capa.Cantidad * capa.Precio);
                                    cantPorRetirar -= capa.Cantidad;
                                    capasPeps.RemoveAt(0);
                                }
                                else
                                {
                                    costoAcumuladoSalida += (cantPorRetirar * capa.Precio);
                                    capasPeps[0] = (capa.Cantidad - cantPorRetirar, capa.Precio);
                                    cantPorRetirar = 0;
                                }
                            }
                            totalSalida = costoAcumuladoSalida;
                            costoUSalida = cantSalida > 0 ? totalSalida / cantSalida : 0;
                        }
                        else
                        {
                            costoUSalida = costoPromedio;
                            totalSalida = cantSalida * costoUSalida;
                        }
                        saldoCant -= cantSalida;
                        saldoCostoTotal -= totalSalida;
                        if (metodo == "PROM") costoPromedio = saldoCant > 0 ? saldoCostoTotal / saldoCant : 0;
                    }

                    // LÓGICA DE SALDO ANTERIOR
                    if (mov.Fecha < fechaInicio)
                    {
                        antCant = saldoCant;
                        antCostoTotal = saldoCostoTotal;
                    }
                    else // MOVIMIENTOS DENTRO DEL RANGO
                    {
                        if (reporte.Count == 0 && fechaInicio > movimientos.Min(x => x.Fecha))
                        {
                            reporte.Add(new
                            {
                                fecha = "-",
                                tipoDoc = "-",
                                doc = "-",
                                motivo = "SALDO ANTERIOR AL " + fechaInicio.ToString("dd/MM/yyyy"),
                                eCant = "-",
                                eCosto = "-",
                                eTotal = "-",
                                sCant = "-",
                                sCosto = "-",
                                sTotal = "-",
                                finalCant = antCant.ToString("N2"),
                                finalCosto = (antCant > 0 ? antCostoTotal / antCant : 0).ToString("N4"),
                                finalTotal = antCostoTotal.ToString("N2"),
                                isHeader = true
                            });
                        }

                        reporte.Add(new
                        {
                            fecha = mov.Fecha.Value.ToString("dd/MM/yyyy"),
                            // AQUÍ USAMOS EL CÓDIGO PARA SUNAT (TABLA 10)
                            tipoDoc = mov.CodigoTipoDoc + " - " + mov.TipoDoc,
                            doc = $"{(mov.SerieDocumento ?? "---")}-{(mov.NumeroDocumento ?? "---")}",
                            // AQUÍ USAMOS EL CÓDIGO DE MOTIVO (TABLA 12) CONCATENADO CON DESCRIPCIÓN
                            motivo = mov.CodigoMotivo + " - " + mov.Motivo,
                            eCant = cantEntrada > 0 ? cantEntrada.ToString("N2") : "-",
                            eCosto = cantEntrada > 0 ? costoUEntrada.ToString("N4") : "-",
                            eTotal = cantEntrada > 0 ? totalEntrada.ToString("N2") : "-",
                            sCant = cantSalida > 0 ? cantSalida.ToString("N2") : "-",
                            sCosto = cantSalida > 0 ? costoUSalida.ToString("N4") : "-",
                            sTotal = cantSalida > 0 ? totalSalida.ToString("N2") : "-",
                            finalCant = saldoCant.ToString("N2"),
                            finalCosto = (metodo == "PEPS" ? (saldoCant > 0 ? saldoCostoTotal / saldoCant : 0) : costoPromedio).ToString("N4"),
                            finalTotal = saldoCostoTotal.ToString("N2"),
                            isHeader = false
                        });
                    }
                }

                // =================================================================================
                // 3. RETORNO CON METADATOS EXTRA
                // =================================================================================

                return Json(new
                {
                    status = true,
                    data = reporte,
                    // Agregamos el objeto extra para llenar la cabecera en el JS
                    infoProducto = new
                    {
                        codigo = datosProducto?.Codigo ?? "S/D",
                        descripcion = datosProducto?.DescripcionProducto ?? "S/D",
                        unidad = (datosProducto?.UnidadCodigo ?? "NIU") + " - " + (datosProducto?.UnidadNombre ?? "UNIDAD"), // Tabla 6
                        tipo = codigoSunatTabla5 + " - " + nombreSunatTabla5, // Tabla 5 Calculada
                        rucEmpresa = datosEmpresa?.Ruc ?? "",
                        razonSocial = datosEmpresa?.RazonSocial ?? "",
                        sucursal = datosEmpresa?.NombreSucursal ?? ""
                    }
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        [HttpPost]
        public async Task<JsonResult> GuardarEntidadRapido(Entidad modelo)
        {
            try
            {
                // 1. Recuperamos el ID de la empresa de la sesión (Opción 3 que elegiste)
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                if (string.IsNullOrEmpty(empresaIdClaim))
                    return Json(new { status = false, message = "Sesión expirada" });

                int empresaId = int.Parse(empresaIdClaim);

                // 2. Validar si ya existe en esta empresa
                var existe = await _context.Entidades
                    .AnyAsync(x => x.Ruc == modelo.Ruc && x.EmpresaId == empresaId);

                if (existe)
                    return Json(new { status = false, message = "El RUC ya está registrado en su empresa." });

                // 3. Asignar valores por defecto
                modelo.EmpresaId = empresaId;
                modelo.Estado = true;

                _context.Entidades.Add(modelo);
                await _context.SaveChangesAsync();

                // 4. Retornamos el ID para seleccionarlo automáticamente en el combo
                return Json(new
                {
                    status = true,
                    message = "Entidad registrada correctamente",
                    id = modelo.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }
    }
}