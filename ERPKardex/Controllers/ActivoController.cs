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

        #region VISTAS (Navegación)

        public IActionResult Index(string tipo = "COMPUTO")
        {
            ViewBag.TipoModulo = tipo;
            ViewBag.EsAdmin = EsAdminGlobal;
            if (tipo == "VEHICULOS") return View("IndexVehiculos");
            return View("Index");
        }

        public IActionResult Registro(string tipo = "COMPUTO")
        {
            ViewBag.TipoModulo = tipo;
            if (tipo == "VEHICULOS") return View("RegistroVehiculo");
            return View("Registro");
        }

        public IActionResult Asignacion(string tipo = "COMPUTO")
        {
            ViewBag.TipoModulo = tipo;
            return View();
        }

        public IActionResult Historial(string tipo = "COMPUTO")
        {
            ViewBag.TipoModulo = tipo;
            if (int.TryParse(Request.Query["id"], out int id))
            {
                ViewBag.ActivoId = id;
            }
            return View();
        }

        // Vista para impresión de actas
        public IActionResult ActaImpresion(int id)
        {
            ViewBag.IdMovimiento = id;
            return View();
        }

        // Vistas Parciales (Modales)
        public IActionResult BitacoraKm(int id)
        {
            ViewBag.ActivoId = id;
            return PartialView("_BitacoraKilometraje");
        }

        public IActionResult GestionDocumental(int id)
        {
            ViewBag.ActivoId = id;
            return PartialView("_GestionDocumental");
        }
        #endregion

        #region API: LISTADO PRINCIPAL (GetActivos)

        [HttpGet]
        public async Task<JsonResult> GetActivos(int? empresaIdFiltro, string tipoModulo = "COMPUTO")
        {
            try
            {
                // 1. Join Inicial para poder filtrar por Grupo
                var query = from a in _context.Activos
                            join g in _context.ActivoGrupos on a.ActivoGrupoId equals g.Id
                            select new { a, g };

                // 2. Filtros de Seguridad y Módulo
                if (EsAdminGlobal)
                {
                    if (empresaIdFiltro.HasValue && empresaIdFiltro.Value > 0)
                        query = query.Where(x => x.a.EmpresaId == empresaIdFiltro.Value);
                }
                else
                {
                    query = query.Where(x => x.a.EmpresaId == EmpresaUsuarioId);
                }

                if (!string.IsNullOrEmpty(tipoModulo))
                {
                    if (tipoModulo == "VEHICULOS")
                        query = query.Where(x => x.g.Nombre == "VEHÍCULOS");
                    else
                        query = query.Where(x => x.g.Nombre != "VEHÍCULOS");
                }

                // 3. Proyección de Datos
                var data = await (from q in query
                                  join t in _context.ActivoTipos on q.a.ActivoTipoId equals t.Id into tj
                                  from t in tj.DefaultIfEmpty()
                                  join m in _context.Marcas on q.a.MarcaId equals m.Id into mj
                                  from m in mj.DefaultIfEmpty()
                                  join mo in _context.Modelos on q.a.ModeloId equals mo.Id into moj
                                  from mo in moj.DefaultIfEmpty()

                                      // Subquery para obtener la última ubicación/responsable
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
                                      Kilometraje = q.a.MedidaActual,
                                      ProxMant = q.a.ProxMantenimiento,
                                      AlertaMant = (tipoModulo == "VEHICULOS" && q.a.MedidaActual >= q.a.ProxMantenimiento) ? "ROJO" : "VERDE",
                                      AsignadoA = (q.a.Situacion == "EN STOCK") ? " EN STOCK" : (p != null) ? p.NombresCompletos : "SIN ASIGNAR",
                                      PersonalId = (p != null) ? p.Id : (int?)null,
                                      Ubicacion = ultimoMov != null ? ultimoMov.UbicacionDestino : "-"
                                  })
                                  .ToListAsync();

                return Json(new { status = true, data = data.OrderBy(x => x.AsignadoA).ToList() });
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
                var docs = await _context.ActivoDocumentos.Where(d => d.ActivoId == id && d.Estado == true).ToListAsync();

                return Json(new { status = true, data = activo, specs = specs, docs = docs });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        #endregion

        #region API: HISTORIAL Y ACTAS (RESTAURADO Y ADAPTADO)

        [HttpGet]
        public async Task<JsonResult> GetHistorial(int activoId)
        {
            try
            {
                // 1. Cabecera del Activo
                var activoHeader = await (from a in _context.Activos
                                          join t in _context.ActivoTipos on a.ActivoTipoId equals t.Id into tj
                                          from t in tj.DefaultIfEmpty()
                                          join m in _context.Marcas on a.MarcaId equals m.Id into mj
                                          from m in mj.DefaultIfEmpty()
                                          join mo in _context.Modelos on a.ModeloId equals mo.Id into moj
                                          from mo in moj.DefaultIfEmpty()
                                          where a.Id == activoId
                                          select new
                                          {
                                              Id = a.Id,
                                              Codigo = a.CodigoInterno, // Placa
                                              Serie = a.Serie,          // VIN
                                              Descripcion = (t.Nombre ?? "") + " " + (m.Nombre ?? "") + " " + (mo.Nombre ?? ""),
                                              Condicion = a.Condicion,
                                              Situacion = a.Situacion,
                                              KmActual = a.MedidaActual, // Dato vital para cabecera de vehículo
                                              Unidad = a.UnidadMedidaUso
                                          }).FirstOrDefaultAsync();

                // 2. Historial de Movimientos (Kardex)
                // CORRECCIÓN: Agregamos d.MedidaRegistro para ver con cuánto KM se entregó/devolvió
                var historial = await (from d in _context.DMovimientosActivo
                                       join m in _context.MovimientosActivo on d.MovimientoActivoId equals m.Id
                                       join p in _context.Personal on m.PersonalId equals p.Id into pj
                                       from p in pj.DefaultIfEmpty()
                                       where d.ActivoId == activoId
                                       orderby m.FechaMovimiento descending
                                       select new
                                       {
                                           Id = m.Id,
                                           CodigoActa = m.CodigoActa,
                                           Fecha = m.FechaMovimiento.HasValue ? m.FechaMovimiento.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                                           Tipo = m.TipoMovimiento,
                                           Responsable = (m.TipoMovimiento == "DEVOLUCION") ? "ALMACÉN / STOCK" : (p != null ? p.NombresCompletos : "SIN ASIGNAR"),
                                           Ubicacion = m.UbicacionDestino,
                                           Observacion = d.ObservacionItem ?? m.Observacion,
                                           Kilometraje = d.MedidaRegistro, // <--- ESTO FALTABA
                                           RutaActa = m.RutaActaPdf,
                                           Estado = m.Estado == true ? "VIGENTE" : "ANULADO"
                                       }).ToListAsync();

                return Json(new { status = true, data = historial, activoInfo = activoHeader });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // [NUEVO] API PARA LISTAR DOCUMENTOS EN LA VISTA DE HISTORIAL
        [HttpGet]
        public async Task<JsonResult> GetDocumentosActivo(int activoId)
        {
            try
            {
                var docs = await _context.ActivoDocumentos
                                         .Where(x => x.ActivoId == activoId && x.Estado == true)
                                         .OrderByDescending(x => x.FechaVencimiento)
                                         .Select(x => new
                                         {
                                             x.Id,
                                             Tipo = x.TipoDocumento,
                                             Nro = x.NroDocumento,
                                             Vence = x.FechaVencimiento.HasValue ? x.FechaVencimiento.Value.ToString("dd/MM/yyyy") : "-",
                                             x.RutaArchivo,
                                             EstadoVenc = (x.FechaVencimiento < DateTime.Now) ? "VENCIDO" : "VIGENTE"
                                         })
                                         .ToListAsync();

                return Json(new { status = true, data = docs });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetDatosActa(int idMovimiento)
        {
            try
            {
                var movimiento = await _context.MovimientosActivo.FindAsync(idMovimiento);
                if (movimiento == null) return Json(new { status = false, message = "Acta no encontrada" });

                bool esDevolucion = movimiento.TipoMovimiento == "DEVOLUCION";
                string tipoActa = esDevolucion ? "ACTA DE DEVOLUCIÓN" : "ACTA DE ENTREGA";

                // Recuperar Items (JOIN MANUAL)
                var items = await (from d in _context.DMovimientosActivo
                                   join a in _context.Activos on d.ActivoId equals a.Id
                                   join t in _context.ActivoTipos on a.ActivoTipoId equals t.Id into tj
                                   from t in tj.DefaultIfEmpty()
                                   join m in _context.Marcas on a.MarcaId equals m.Id into mj
                                   from m in mj.DefaultIfEmpty()
                                   join mo in _context.Modelos on a.ModeloId equals mo.Id into moj
                                   from mo in moj.DefaultIfEmpty()
                                   where d.MovimientoActivoId == idMovimiento
                                   select new
                                   {
                                       a.Id,
                                       Tipo = t.Nombre,
                                       Marca = m.Nombre,
                                       Modelo = mo.Nombre,
                                       Serie = a.Serie,
                                       Codigo = a.CodigoInterno,
                                       Condicion = d.CondicionItem,
                                       Specs = _context.ActivoEspecificaciones.Where(s => s.ActivoId == a.Id).ToList()
                                   }).ToListAsync();

                var listaItems = items.Select(i => new
                {
                    Codigo = i.Codigo,
                    Equipo = $"{i.Tipo} {i.Marca}".Trim(),
                    Modelo = i.Modelo,
                    Serie = i.Serie,
                    Condicion = i.Condicion,
                    Caracteristicas = string.Join(", ", i.Specs.Select(s => $"{s.Clave}: {s.Valor}"))
                }).ToList();

                // Datos Personales (Manuales sin navegación)
                string nombreEmisor = "", dniEmisor = "", cargoEmisor = "", empresaEmisor = "";
                string nombreReceptor = "", dniReceptor = "", cargoReceptor = "", empresaReceptor = "";

                // Info Usuario Sistema (TI)
                var userSys = await _context.Usuarios.FindAsync(movimiento.UsuarioRegistroId);
                // Info Empresa (Dueña del activo)
                var empSys = await _context.Empresas.FindAsync(movimiento.EmpresaId);

                string tiNombre = userSys?.Nombre ?? "TI/SISTEMAS";
                string tiDni = userSys?.Dni ?? "";
                string tiEmpresa = empSys?.RazonSocial ?? "YNNOVACORP";

                if (esDevolucion)
                {
                    // Devuelve: Personal -> Recibe: TI
                    if (movimiento.PersonalId.HasValue)
                    {
                        var per = await _context.Personal.FindAsync(movimiento.PersonalId);
                        var empPer = await _context.Empresas.FindAsync(per?.EmpresaId);
                        nombreEmisor = per?.NombresCompletos;
                        dniEmisor = per?.Dni;
                        cargoEmisor = per?.Cargo;
                        empresaEmisor = empPer?.RazonSocial;
                    }
                    nombreReceptor = tiNombre;
                    dniReceptor = tiDni;
                    cargoReceptor = "TI";
                    empresaReceptor = tiEmpresa;
                }
                else
                {
                    // Entrega: TI -> Recibe: Personal
                    nombreEmisor = tiNombre;
                    dniEmisor = tiDni;
                    cargoEmisor = "TI";
                    empresaEmisor = tiEmpresa;

                    if (movimiento.PersonalId.HasValue)
                    {
                        var per = await _context.Personal.FindAsync(movimiento.PersonalId);
                        var empPer = await _context.Empresas.FindAsync(per?.EmpresaId);
                        nombreReceptor = per?.NombresCompletos;
                        dniReceptor = per?.Dni;
                        cargoReceptor = per?.Cargo;
                        empresaReceptor = empPer?.RazonSocial;
                    }
                }

                return Json(new
                {
                    status = true,
                    data = new
                    {
                        Titulo = tipoActa,
                        CodigoActa = movimiento.CodigoActa,
                        Fecha = movimiento.FechaMovimiento?.ToString("dd/MM/yyyy"),
                        Ubicacion = movimiento.UbicacionDestino,
                        Emisor = new { Nombre = nombreEmisor, Dni = dniEmisor, Cargo = cargoEmisor, Empresa = empresaEmisor },
                        Receptor = new { Nombre = nombreReceptor, Dni = dniReceptor, Cargo = cargoReceptor, Empresa = empresaReceptor },
                        Items = listaItems
                    }
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> SubirActa(int idMovimiento, IFormFile archivo)
        {
            try
            {
                if (archivo == null) return Json(new { status = false, message = "Sin archivo" });
                var mov = await _context.MovimientosActivo.FindAsync(idMovimiento);

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

        #region API: REGISTRO ACTIVO (COMPATIBLE CON AMBOS)

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

        [HttpPost]
        public async Task<JsonResult> GuardarActivo(ActivoRegistroModel model)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    string codigoFinal = model.CodigoInterno;
                    if (string.IsNullOrEmpty(codigoFinal))
                    {
                        codigoFinal = "ACT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    }

                    // Detectar si es vehículo para asignar unidad
                    string unidad = "UND";
                    var grupo = await _context.ActivoGrupos.FindAsync(model.GrupoId);
                    if (grupo != null && grupo.Nombre == "VEHÍCULOS") unidad = "KM";

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
                        // Datos Vehículo
                        AnioFabricacion = model.Anio,
                        Color = model.Color,
                        ModalidadAdquisicion = model.Modalidad,
                        MedidaActual = model.KilometrajeInicial ?? 0,
                        ProxMantenimiento = (model.KilometrajeInicial ?? 0) + (model.FrecuenciaMant ?? 5000),
                        FrecuenciaMant = model.FrecuenciaMant ?? 5000,
                        UnidadMedidaUso = unidad
                    };

                    _context.Activos.Add(activo);
                    await _context.SaveChangesAsync();

                    if (model.Specs != null)
                    {
                        foreach (var s in model.Specs) { s.ActivoId = activo.Id; _context.ActivoEspecificaciones.Add(s); }
                        await _context.SaveChangesAsync();
                    }

                    // Historial Inicial de KM
                    if (unidad == "KM" && activo.MedidaActual > 0)
                    {
                        var hist = new ActivoHistorialMedida
                        {
                            ActivoId = activo.Id,
                            FechaLectura = DateTime.Now,
                            ValorMedida = activo.MedidaActual,
                            OrigenDato = "REGISTRO_INICIAL",
                            Observacion = "Inicio",
                            UsuarioRegistroId = UsuarioActualId,
                            Estado = true
                        };
                        _context.ActivoHistorialMedidas.Add(hist);
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
                        MedidaRegistro = activo.MedidaActual
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
        #endregion

        #region API: GESTIÓN DE MOVIMIENTOS (ENTREGA/DEVOLUCIÓN)

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
            public string Observacion { get; set; }
            public decimal? NuevaMedida { get; set; }
        }

        [HttpPost]
        public async Task<JsonResult> GuardarMovimiento([FromBody] MovimientoDTO data)
        {
            if (data.Items == null || !data.Items.Any())
                return Json(new { status = false, message = "No se han seleccionado activos." });

            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    int? responsableIdDetectado = data.PersonalId;

                    if (data.TipoMovimiento == "DEVOLUCION")
                    {
                        int primerActivoId = data.Items.First().ActivoId;
                        var ultimoMov = await (from d in _context.DMovimientosActivo
                                               join m in _context.MovimientosActivo on d.MovimientoActivoId equals m.Id
                                               where d.ActivoId == primerActivoId && m.Estado == true
                                               orderby m.FechaMovimiento descending, m.Id descending
                                               select m.PersonalId).FirstOrDefaultAsync();

                        responsableIdDetectado = ultimoMov;
                        if (responsableIdDetectado == null) return Json(new { status = false, message = "No se detectó responsable previo." });
                    }

                    var cabecera = new MovimientoActivo
                    {
                        CodigoActa = "ACT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                        TipoMovimiento = data.TipoMovimiento,
                        FechaMovimiento = DateTime.Now,
                        EmpresaId = EmpresaUsuarioId,
                        UsuarioRegistroId = UsuarioActualId,
                        PersonalId = responsableIdDetectado,
                        UbicacionDestino = data.Ubicacion,
                        Observacion = data.Observacion,
                        Estado = true
                    };

                    _context.MovimientosActivo.Add(cabecera);
                    await _context.SaveChangesAsync();

                    foreach (var item in data.Items)
                    {
                        var activoDb = await _context.Activos.FindAsync(item.ActivoId);
                        if (activoDb == null) continue;

                        // Validar KM (Solo para Vehículos)
                        if (activoDb.UnidadMedidaUso == "KM" && item.NuevaMedida.HasValue)
                        {
                            if (item.NuevaMedida < activoDb.MedidaActual)
                                throw new Exception($"Error: El KM ingresado ({item.NuevaMedida}) es menor al actual ({activoDb.MedidaActual}).");

                            activoDb.MedidaActual = item.NuevaMedida.Value;

                            var hist = new ActivoHistorialMedida
                            {
                                ActivoId = activoDb.Id,
                                FechaLectura = DateTime.Now,
                                ValorMedida = item.NuevaMedida.Value,
                                OrigenDato = data.TipoMovimiento,
                                Observacion = "Registro por acta " + cabecera.CodigoActa,
                                UsuarioRegistroId = UsuarioActualId,
                                Estado = true
                            };
                            _context.ActivoHistorialMedidas.Add(hist);
                        }

                        var detalle = new DMovimientoActivo
                        {
                            MovimientoActivoId = cabecera.Id,
                            ActivoId = item.ActivoId,
                            CondicionItem = item.Condicion,
                            ObservacionItem = item.Observacion,
                            MedidaRegistro = item.NuevaMedida ?? 0
                        };
                        _context.DMovimientosActivo.Add(detalle);

                        activoDb.Condicion = item.Condicion;
                        if (data.TipoMovimiento == "ENTREGA") activoDb.Situacion = "EN USO";
                        else activoDb.Situacion = "EN STOCK";
                    }

                    await _context.SaveChangesAsync();
                    tx.Commit();

                    return Json(new { status = true, message = "Movimiento registrado.", idMovimiento = cabecera.Id });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }
        #endregion

        #region API: DOCUMENTOS Y BITÁCORA

        [HttpPost]
        public async Task<JsonResult> GuardarDocumento(int activoId, string tipoDoc, string nroDoc, DateTime fechaVenc, IFormFile archivo)
        {
            try
            {
                string rutaRelativa = null;
                if (archivo != null)
                {
                    var path = Path.Combine(_env.WebRootPath, "uploads", "vehiculos");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    string nombreFile = $"{activoId}_{tipoDoc}_{DateTime.Now.Ticks}{Path.GetExtension(archivo.FileName)}";
                    using (var stream = new FileStream(Path.Combine(path, nombreFile), FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }
                    rutaRelativa = "uploads/vehiculos/" + nombreFile;
                }

                var doc = new ActivoDocumento
                {
                    ActivoId = activoId,
                    TipoDocumento = tipoDoc,
                    NroDocumento = nroDoc,
                    FechaVencimiento = fechaVenc,
                    FechaEmision = DateTime.Now,
                    RutaArchivo = rutaRelativa,
                    Estado = true
                };
                _context.ActivoDocumentos.Add(doc);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Documento registrado." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetHistorialMedidas(int activoId)
        {
            var data = _context.ActivoHistorialMedidas
                .Where(h => h.ActivoId == activoId && h.Estado == true)
                .OrderByDescending(h => h.FechaLectura)
                .Select(h => new
                {
                    Fecha = h.FechaLectura.HasValue ? h.FechaLectura.Value.ToString("dd/MM/yyyy HH:mm") : "-",
                    Valor = h.ValorMedida,
                    Origen = h.OrigenDato,
                    Obs = h.Observacion,
                    Usuario = _context.Usuarios.FirstOrDefault(u => u.Id == h.UsuarioRegistroId).Nombre
                }).ToList();
            return Json(new { status = true, data = data });
        }

        [HttpPost]
        public async Task<JsonResult> RegistrarMedidaManual(int activoId, decimal medida, string observacion)
        {
            try
            {
                var activo = await _context.Activos.FindAsync(activoId);
                if (activo == null) return Json(new { status = false, message = "Activo no encontrado" });
                if (medida < activo.MedidaActual) return Json(new { status = false, message = "El valor no puede ser menor al actual." });

                activo.MedidaActual = medida;
                var hist = new ActivoHistorialMedida
                {
                    ActivoId = activoId,
                    FechaLectura = DateTime.Now,
                    ValorMedida = medida,
                    OrigenDato = "CONTROL_SEMANAL",
                    Observacion = observacion,
                    UsuarioRegistroId = UsuarioActualId,
                    Estado = true
                };
                _context.ActivoHistorialMedidas.Add(hist);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Lectura registrada." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region COMBOS

        [HttpGet]
        public JsonResult GetCombosRegistro(string tipoModulo = "COMPUTO")
        {
            var grupos = _context.ActivoGrupos.Where(x => x.Estado == true).ToList();
            if (tipoModulo == "VEHICULOS") grupos = grupos.Where(g => g.Nombre == "VEHÍCULOS").ToList();
            else grupos = grupos.Where(g => g.Nombre != "VEHÍCULOS").ToList();

            return Json(new
            {
                status = true,
                grupos = grupos,
                tipos = _context.ActivoTipos.Where(x => x.Estado == true).ToList(),
                marcas = _context.Marcas.Where(x => x.Estado == true).ToList(),
                modelos = _context.Modelos.Where(x => x.Estado == true).ToList()
            });
        }

        [HttpGet]
        public JsonResult GetPersonalCombo()
        {
            var p = _context.Personal.Where(x => x.Estado == true)
                .Select(x => new { x.Id, x.NombresCompletos, x.EmpresaId })
                .OrderBy(x => x.NombresCompletos).ToList();

            if (!EsAdminGlobal) p = p.Where(p => p.EmpresaId == EmpresaUsuarioId).ToList();
            return Json(new { status = true, data = p });
        }

        [HttpGet]
        public JsonResult GetModelosByMarca(int marcaId)
        {
            var modelos = _context.Modelos.Where(x => x.MarcaId == marcaId && x.Estado == true && x.EmpresaId == EmpresaUsuarioId)
                .Select(x => new { x.Id, x.Nombre }).OrderBy(x => x.Nombre).ToList();
            return Json(new { status = true, data = modelos });
        }

        [HttpGet]
        public async Task<JsonResult> GetUbicaciones()
        {
            var ubicaciones = await _context.MovimientosActivo.Where(x => !string.IsNullOrEmpty(x.UbicacionDestino))
                .Select(x => x.UbicacionDestino).Distinct().OrderBy(x => x).ToListAsync();
            return Json(new { status = true, data = ubicaciones });
        }
        #endregion
    }
}