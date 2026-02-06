using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class ActivoController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ActivoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==========================================================================================
        // REGIÓN 1: MÓDULO DE CÓMPUTO (LÓGICA ORIGINAL LIMPIA)
        // ==========================================================================================
        #region MÓDULO CÓMPUTO

        // 1. VISTAS DE CÓMPUTO
        public IActionResult IndexComputo()
        {
            ViewBag.TipoModulo = "COMPUTO";
            ViewBag.EsAdmin = EsAdminGlobal;
            return View("Index"); // Usa tu vista original Index.cshtml
        }

        public IActionResult RegistroComputo()
        {
            ViewBag.TipoModulo = "COMPUTO";
            return View("Registro"); // Usa tu vista original Registro.cshtml
        }

        public IActionResult HistorialComputo(int id)
        {
            ViewBag.TipoModulo = "COMPUTO";
            ViewBag.ActivoId = id;
            return View("Historial"); // Usa tu vista original Historial.cshtml
        }

        public IActionResult AsignacionComputo()
        {
            ViewBag.TipoModulo = "COMPUTO";
            return View("Asignacion"); // Usa tu vista original Asignacion.cshtml
        }

        public IActionResult ActaImpresion(int id)
        {
            ViewBag.IdMovimiento = id;
            return View();
        }

        // 2. APIS DE CÓMPUTO (GetActivos ORIGINAL LIMPIO)
        [HttpGet]
        public async Task<JsonResult> GetActivos(int? empresaIdFiltro)
        {
            try
            {
                // Join Inicial
                var query = from a in _context.Activos
                            join g in _context.ActivoGrupos on a.ActivoGrupoId equals g.Id
                            select new { a, g };

                // Filtros de Seguridad
                if (EsAdminGlobal)
                {
                    if (empresaIdFiltro.HasValue && empresaIdFiltro.Value > 0)
                        query = query.Where(x => x.a.EmpresaId == empresaIdFiltro.Value);
                }
                else
                {
                    query = query.Where(x => x.a.EmpresaId == EmpresaUsuarioId);
                }

                // FILTRO ESTRICTO: NO MOSTRAR VEHÍCULOS AQUÍ
                query = query.Where(x => x.g.Nombre != "VEHÍCULOS" && x.g.Nombre != "FLOTA");

                // Proyección de Datos (Tu lógica original)
                var data = await (from q in query
                                  join t in _context.ActivoTipos on q.a.ActivoTipoId equals t.Id into tj
                                  from t in tj.DefaultIfEmpty()
                                  join m in _context.Marcas on q.a.MarcaId equals m.Id into mj
                                  from m in mj.DefaultIfEmpty()
                                  join mo in _context.Modelos on q.a.ModeloId equals mo.Id into moj
                                  from mo in moj.DefaultIfEmpty()
                                      // Subquery última ubicación
                                  let ultimoMov = (from d in _context.DMovimientosActivo
                                                   join mov in _context.MovimientosActivo on d.MovimientoActivoId equals mov.Id
                                                   where d.ActivoId == q.a.Id && mov.Estado == true
                                                   orderby mov.FechaMovimiento descending, mov.Id descending
                                                   select new { mov.PersonalId, mov.UbicacionDestino }).FirstOrDefault()
                                  join p in _context.Personal on ultimoMov.PersonalId equals p.Id into pj
                                  from p in pj.DefaultIfEmpty()
                                  select new
                                  {
                                      Id = q.a.Id,
                                      Codigo = q.a.CodigoInterno ?? "S/C",
                                      Descripcion = (t.Nombre ?? "") + " " + (m.Nombre ?? "") + " " + (mo.Nombre ?? ""),
                                      Serie = q.a.Serie ?? "-",
                                      Condicion = q.a.Condicion,
                                      Situacion = q.a.Situacion,
                                      AsignadoA = (q.a.Situacion == "EN STOCK") ? " EN STOCK" : (p != null) ? p.NombresCompletos : "SIN ASIGNAR",
                                      Ubicacion = ultimoMov != null ? ultimoMov.UbicacionDestino : "-"
                                  })
                                  .ToListAsync();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetActivoById(int id)
        {
            try
            {
                var activo = await _context.Activos.FindAsync(id);
                if (activo == null) return Json(new { status = false, message = "No encontrado" });

                var specs = await _context.ActivoEspecificaciones.Where(s => s.ActivoId == id).ToListAsync();
                // Solo traemos documentos básicos
                var docs = await _context.ActivoDocumentos.Where(d => d.ActivoId == id && d.Estado == true).ToListAsync();

                return Json(new { status = true, data = activo, specs = specs, docs = docs });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // GUARDAR ACTIVO DE CÓMPUTO (Original)
        [HttpPost]
        public async Task<JsonResult> GuardarActivo(ActivoRegistroModel model)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    string codigoFinal = model.CodigoInterno;
                    if (string.IsNullOrEmpty(codigoFinal))
                        codigoFinal = "ACT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                    var activo = new Activo
                    {
                        CodigoInterno = codigoFinal,
                        ActivoGrupoId = model.GrupoId,
                        ActivoTipoId = model.TipoId,
                        MarcaId = model.MarcaId,
                        ModeloId = model.ModeloId,
                        Serie = model.Serie,
                        Condicion = model.Condicion ?? "OPERATIVO",
                        Situacion = "EN STOCK",
                        EmpresaId = EmpresaUsuarioId,
                        FechaRegistro = DateTime.Now,
                        Estado = true,
                        // Valores por defecto para cómputo
                        UnidadMedidaUso = "UND",
                        MedidaActual = 0,
                        ProxMantenimiento = 0,
                        FrecuenciaMant = 0
                    };

                    _context.Activos.Add(activo);
                    await _context.SaveChangesAsync();

                    if (model.Specs != null)
                    {
                        foreach (var s in model.Specs) { s.ActivoId = activo.Id; _context.ActivoEspecificaciones.Add(s); }
                        await _context.SaveChangesAsync();
                    }

                    // Movimiento Inicial (Alta)
                    var mov = new MovimientoActivo
                    {
                        CodigoActa = "ALTA-" + activo.Id,
                        TipoMovimiento = "ALTA",
                        FechaMovimiento = DateTime.Now,
                        EmpresaId = EmpresaUsuarioId,
                        UsuarioRegistroId = UsuarioActualId,
                        UbicacionDestino = "STOCK",
                        Observacion = "INGRESO INICIAL",
                        Estado = true
                    };
                    _context.MovimientosActivo.Add(mov);
                    await _context.SaveChangesAsync();

                    var det = new DMovimientoActivo
                    {
                        MovimientoActivoId = mov.Id,
                        ActivoId = activo.Id,
                        CondicionItem = activo.Condicion,
                        ObservacionItem = "Nuevo",
                        MedidaRegistro = 0
                    };
                    _context.DMovimientosActivo.Add(det);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    return Json(new { status = true, message = "Activo registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        // GUARDAR MOVIMIENTO DE CÓMPUTO (Original)
        [HttpPost]
        public async Task<JsonResult> GuardarMovimiento([FromBody] MovimientoDTO dto)
        {
            using (var transaccion = _context.Database.BeginTransaction())
            {
                try
                {
                    var mov = new MovimientoActivo
                    {
                        CodigoActa = "MOV-" + DateTime.Now.Ticks.ToString().Substring(10),
                        TipoMovimiento = dto.TipoMovimiento,
                        FechaMovimiento = DateTime.Now,
                        PersonalId = dto.PersonalId,
                        EmpresaId = EmpresaUsuarioId,
                        UsuarioRegistroId = UsuarioActualId,
                        UbicacionDestino = dto.Ubicacion,
                        Observacion = dto.Observacion,
                        Estado = true
                    };
                    _context.MovimientosActivo.Add(mov);
                    await _context.SaveChangesAsync();

                    foreach (var item in dto.Items)
                    {
                        var det = new DMovimientoActivo
                        {
                            MovimientoActivoId = mov.Id,
                            ActivoId = item.ActivoId,
                            CondicionItem = item.Condicion,
                            ObservacionItem = item.Observacion,
                            MedidaRegistro = 0 // Cómputo no usa KM
                        };
                        _context.DMovimientosActivo.Add(det);

                        // Actualizar Activo
                        var activo = await _context.Activos.FindAsync(item.ActivoId);
                        if (activo != null)
                        {
                            activo.Condicion = item.Condicion;
                            if (dto.TipoMovimiento == "ENTREGA") activo.Situacion = "EN USO";
                            else if (dto.TipoMovimiento == "DEVOLUCION") activo.Situacion = "EN STOCK";
                        }
                    }
                    await _context.SaveChangesAsync();
                    transaccion.Commit();

                    return Json(new { status = true, message = "Movimiento registrado", idMovimiento = mov.Id });
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        #endregion

        // ==========================================================================================
        // REGIÓN 2: MÓDULO DE FLOTA VEHICULAR (LÓGICA NUEVA)
        // ==========================================================================================
        #region MÓDULO FLOTA

        // 1. VISTAS DE FLOTA
        public IActionResult IndexFlota()
        {
            ViewBag.TipoModulo = "VEHICULOS";
            return View("IndexVehiculos"); // Carga IndexVehiculos.cshtml
        }

        public IActionResult RegistroFlota()
        {
            ViewBag.TipoModulo = "VEHICULOS";
            // Asegúrate de tener una vista llamada RegistroVehiculo.cshtml o adapta la existente
            return View("RegistroVehiculo");
        }

        public IActionResult HistorialFlota(int id)
        {
            var activo = _context.Activos.Find(id);
            if (activo == null) return NotFound();

            ViewBag.TipoModulo = "VEHICULOS";
            ViewBag.ActivoId = id;
            ViewBag.Placa = activo.CodigoInterno;
            ViewBag.Estado = activo.Condicion;
            return View("HistorialVehiculo"); // Carga HistorialVehiculo.cshtml
        }

        public IActionResult AsignacionFlota()
        {
            ViewBag.TipoModulo = "VEHICULOS";
            return View("AsignacionVehiculo"); // Carga AsignacionVehiculo.cshtml
        }


        // 2. APIS DE FLOTA (NUEVAS TABLAS)

        // Listado para IndexVehiculos
        [HttpGet]
        public async Task<JsonResult> GetListadoFlota()
        {
            try
            {
                var data = await (from a in _context.Activos
                                  join f in _context.VehiculoFichas on a.Id equals f.ActivoId into fichas
                                  from ficha in fichas.DefaultIfEmpty()
                                  join g in _context.ActivoGrupos on a.ActivoGrupoId equals g.Id

                                  // Subquery responsable
                                  let ultimoMov = (from d in _context.DMovimientosActivo
                                                   join mov in _context.MovimientosActivo on d.MovimientoActivoId equals mov.Id
                                                   join p in _context.Personal on mov.PersonalId equals p.Id
                                                   where d.ActivoId == a.Id && mov.Estado == true
                                                   orderby mov.FechaMovimiento descending
                                                   select new { Responsable = p.NombresCompletos }).FirstOrDefault()

                                  where a.Estado == true && (g.Nombre == "VEHÍCULOS" || g.Nombre == "FLOTA")
                                  orderby a.CodigoInterno ascending
                                  select new
                                  {
                                      id = a.Id,
                                      placa = a.CodigoInterno,
                                      descripcion = ficha != null ? (ficha.Marca + " " + ficha.Modelo) : "SIN FICHA",
                                      asignadoA = (ultimoMov != null) ? ultimoMov.Responsable : (a.Situacion == "EN STOCK" ? "EN STOCK" : "SIN ASIGNAR"),
                                      kilometraje = a.MedidaActual,
                                      proxMant = a.ProxMantenimiento,
                                      alertaMant = (a.MedidaActual >= a.ProxMantenimiento) ? "ROJO" : "VERDE",
                                      estado = a.Condicion
                                  }).ToListAsync();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // Detalle Completo para HistorialVehiculo
        [HttpGet]
        public async Task<JsonResult> GetDetalleFlota(int id)
        {
            try
            {
                var ficha = await _context.VehiculoFichas.FirstOrDefaultAsync(x => x.ActivoId == id);
                var gps = await _context.VehiculoGpsList.FirstOrDefaultAsync(x => x.ActivoId == id);
                var seguro = await _context.VehiculoSeguros.Where(x => x.ActivoId == id).OrderByDescending(x => x.VigenciaFin).FirstOrDefaultAsync();
                var mant = await _context.VehiculoMantenimientos.Where(x => x.ActivoId == id).OrderByDescending(x => x.FechaServicio).ToListAsync();

                var docs = await _context.ActivoDocumentos
                                   .Where(x => x.ActivoId == id && x.Estado == true)
                                   .Select(x => new
                                   {
                                       tipo = x.TipoDocumento,
                                       nro = x.NroDocumento,
                                       vence = x.FechaVencimiento.HasValue ? x.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "-",
                                       estadoVenc = (x.FechaVencimiento < DateTime.Now) ? "VENCIDO" : "VIGENTE",
                                       ruta = x.RutaArchivo
                                   }).ToListAsync();

                return Json(new { status = true, ficha, gps, seguro, mantenimientos = mant, documentos = docs });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // GUARDAR NUEVO VEHÍCULO
        [HttpPost]
        public async Task<JsonResult> GuardarVehiculo(VehiculoFicha ficha, decimal kmInicial, int frecuenciaMant)
        {
            using (var transaccion = _context.Database.BeginTransaction())
            {
                try
                {
                    var activo = new Activo
                    {
                        CodigoInterno = ficha.Placa.ToUpper(),
                        Serie = ficha.VinChasis,
                        ActivoGrupoId = 7, // OJO: ID del grupo vehículos en tu BD
                        Condicion = "OPERATIVO",
                        Situacion = "EN STOCK",
                        MedidaActual = kmInicial,
                        UnidadMedidaUso = "KM",
                        ProxMantenimiento = kmInicial + frecuenciaMant,
                        FrecuenciaMant = frecuenciaMant,
                        EmpresaId = EmpresaUsuarioId,
                        Estado = true,
                        FechaRegistro = DateTime.Now
                    };
                    _context.Activos.Add(activo);
                    await _context.SaveChangesAsync();

                    ficha.ActivoId = activo.Id;
                    ficha.Estado = true;
                    _context.VehiculoFichas.Add(ficha);

                    _context.ActivoHistorialMedidas.Add(new ActivoHistorialMedida
                    {
                        ActivoId = activo.Id,
                        FechaLectura = DateTime.Now,
                        ValorMedida = kmInicial,
                        OrigenDato = "REGISTRO_INICIAL",
                        Observacion = "Ingreso al sistema",
                        UsuarioRegistroId = UsuarioActualId,
                        Estado = true
                    });

                    await _context.SaveChangesAsync();
                    transaccion.Commit();
                    return Json(new { status = true, message = "Vehículo registrado" });
                }
                catch (Exception ex) { transaccion.Rollback(); return Json(new { status = false, message = ex.Message }); }
            }
        }

        // REGISTRAR MANTENIMIENTO (Nueva Tabla)
        [HttpPost]
        public async Task<JsonResult> GuardarMantenimiento(VehiculoMantenimiento model)
        {
            try
            {
                model.Estado = true;
                _context.VehiculoMantenimientos.Add(model);

                if (model.Tipo == "PREVENTIVO")
                {
                    var activo = await _context.Activos.FindAsync(model.ActivoId);
                    if (activo != null)
                    {
                        activo.ProxMantenimiento = (model.KmRealServicio ?? activo.MedidaActual) + activo.FrecuenciaMant;
                        activo.Condicion = "OPERATIVO";
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Mantenimiento registrado" });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // REGISTRAR MOVIMIENTO FLOTA (Actualiza KM y Estado)
        [HttpPost]
        public async Task<JsonResult> GuardarMovimientoFlota([FromBody] MovimientoDTO dto)
        {
            using (var transaccion = _context.Database.BeginTransaction())
            {
                try
                {
                    var mov = new MovimientoActivo
                    {
                        CodigoActa = "MOV-FLOTA-" + DateTime.Now.Ticks.ToString().Substring(12),
                        TipoMovimiento = dto.TipoMovimiento,
                        FechaMovimiento = DateTime.Now,
                        PersonalId = dto.PersonalId,
                        EmpresaId = EmpresaUsuarioId,
                        UsuarioRegistroId = UsuarioActualId,
                        UbicacionDestino = dto.Ubicacion,
                        Observacion = dto.Observacion,
                        Estado = true
                    };
                    _context.MovimientosActivo.Add(mov);
                    await _context.SaveChangesAsync();

                    foreach (var item in dto.Items)
                    {
                        _context.DMovimientosActivo.Add(new DMovimientoActivo
                        {
                            MovimientoActivoId = mov.Id,
                            ActivoId = item.ActivoId,
                            CondicionItem = item.Condicion,
                            MedidaRegistro = item.NuevaMedida.Value,
                            ObservacionItem = item.Observacion
                        });

                        var activo = await _context.Activos.FindAsync(item.ActivoId);
                        if (activo != null)
                        {
                            activo.Condicion = item.Condicion;
                            if (dto.TipoMovimiento == "ENTREGA") activo.Situacion = "EN USO";
                            else activo.Situacion = "EN STOCK";

                            if (item.NuevaMedida > activo.MedidaActual)
                            {
                                activo.MedidaActual = item.NuevaMedida ?? activo.MedidaActual;
                                _context.ActivoHistorialMedidas.Add(new ActivoHistorialMedida
                                {
                                    ActivoId = activo.Id,
                                    FechaLectura = DateTime.Now,
                                    ValorMedida = activo.MedidaActual,
                                    OrigenDato = "MOVIMIENTO_" + dto.TipoMovimiento,
                                    UsuarioRegistroId = UsuarioActualId,
                                    Estado = true
                                });
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                    transaccion.Commit();
                    return Json(new { status = true, message = "Movimiento registrado", idMovimiento = mov.Id });
                }
                catch (Exception ex) { transaccion.Rollback(); return Json(new { status = false, message = ex.Message }); }
            }
        }

        #endregion


        // ==========================================================================================
        // REGIÓN 3: COMUNES (BITÁCORA, DOCUMENTOS, ACTAS) - Funciona para ambos
        // ==========================================================================================
        #region COMUNES

        // Vistas Parciales
        public IActionResult BitacoraKm(int id) { ViewBag.ActivoId = id; return PartialView("_BitacoraKilometraje"); }
        public IActionResult GestionDocumental(int id) { ViewBag.ActivoId = id; return PartialView("_GestionDocumental"); }

        // API Bitácora
        [HttpGet]
        public async Task<JsonResult> GetHistorialMedidas(int activoId)
        {
            var data = await _context.ActivoHistorialMedidas
                                     .Where(x => x.ActivoId == activoId)
                                     .OrderByDescending(x => x.FechaLectura)
                                     .Select(x => new
                                     {
                                         fecha = x.FechaLectura.Value.ToString("dd/MM/yyyy HH:mm"),
                                         valor = x.ValorMedida,
                                         origen = x.OrigenDato,
                                         obs = x.Observacion,
                                         usuario = "SISTEMA"
                                     }).ToListAsync();
            return Json(new { status = true, data = data });
        }

        [HttpPost]
        public async Task<JsonResult> RegistrarMedidaManual(int activoId, decimal medida, string observacion)
        {
            try
            {
                var activo = await _context.Activos.FindAsync(activoId);
                if (medida < activo.MedidaActual) return Json(new { status = false, message = "El KM no puede disminuir." });

                _context.ActivoHistorialMedidas.Add(new ActivoHistorialMedida
                {
                    ActivoId = activoId,
                    FechaLectura = DateTime.Now,
                    ValorMedida = medida,
                    OrigenDato = "CONTROL_MANUAL",
                    Observacion = observacion,
                    UsuarioRegistroId = UsuarioActualId,
                    Estado = true
                });

                activo.MedidaActual = medida;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Lectura registrada" });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // API Documentos
        [HttpPost]
        public async Task<JsonResult> SubirDocumento(int activoId, string tipoDoc, string nroDoc, DateTime? vencimiento, IFormFile archivo)
        {
            try
            {
                if (archivo == null) return Json(new { status = false, message = "Archivo requerido" });

                var path = Path.Combine(_env.WebRootPath, "uploads", "docs", activoId.ToString());
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string fileName = $"{tipoDoc}_{DateTime.Now.Ticks}{Path.GetExtension(archivo.FileName)}";
                using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                _context.ActivoDocumentos.Add(new ActivoDocumento
                {
                    ActivoId = activoId,
                    TipoDocumento = tipoDoc,
                    NroDocumento = nroDoc,
                    FechaVencimiento = vencimiento,
                    RutaArchivo = $"uploads/docs/{activoId}/{fileName}",
                    Estado = true
                });
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Documento subido" });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetDocumentosActivo(int activoId)
        {
            var data = await _context.ActivoDocumentos.Where(x => x.ActivoId == activoId && x.Estado == true)
                .OrderBy(x => x.FechaVencimiento).Select(x => new
                {
                    tipo = x.TipoDocumento,
                    nro = x.NroDocumento,
                    vence = x.FechaVencimiento.HasValue ? x.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "-",
                    estadoVenc = (x.FechaVencimiento < DateTime.Now) ? "VENCIDO" : "VIGENTE",
                    ruta = x.RutaArchivo
                }).ToListAsync();
            return Json(new { status = true, data = data });
        }

        // Subir Acta de Entrega/Devolución
        [HttpPost]
        public async Task<JsonResult> SubirActa(int idMovimiento, IFormFile archivo)
        {
            try
            {
                if (archivo == null) return Json(new { status = false, message = "Sin archivo" });
                var mov = await _context.MovimientosActivo.FindAsync(idMovimiento);
                if (mov == null) return Json(new { status = false, message = "Movimiento no existe" });

                var path = Path.Combine(_env.WebRootPath, "uploads", "actas");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string fileName = $"ACTA_{mov.Id}_{DateTime.Now.Ticks}.pdf";
                using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                mov.RutaActaPdf = "uploads/actas/" + fileName;
                await _context.SaveChangesAsync();

                return Json(new { status = true, message = "Archivo subido" });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        // CLASES DTO AUXILIARES PARA EL POST
        public class ActivoRegistroModel
        {
            public int? GrupoId { get; set; }
            public int? TipoId { get; set; }
            public int? MarcaId { get; set; }
            public int? ModeloId { get; set; }
            public string CodigoInterno { get; set; }
            public string Serie { get; set; }
            public string Condicion { get; set; }
            public int? Anio { get; set; }
            public string Color { get; set; }
            public string Modalidad { get; set; }
            public decimal? KilometrajeInicial { get; set; }
            public decimal? FrecuenciaMant { get; set; }
            public List<ActivoEspecificacion> Specs { get; set; }
        }

        public class MovimientoDTO
        {
            public string TipoMovimiento { get; set; }
            public int? PersonalId { get; set; }
            public string Ubicacion { get; set; }
            public string Observacion { get; set; }
            public List<ItemMovimientoDTO> Items { get; set; }
        }
        public class ItemMovimientoDTO
        {
            public int ActivoId { get; set; }
            public string Condicion { get; set; }
            public decimal? NuevaMedida { get; set; }
            public string Observacion { get; set; }
        }
    }
}