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

        #region VISTAS
        public IActionResult Index()
        {
            // Pasamos la bandera calculada en BaseController
            ViewBag.EsAdmin = EsAdminGlobal;
            return View();
        }

        public IActionResult Registro() => View();

        public IActionResult Asignacion() => View();

        public IActionResult Historial()
        {
            // Recuperamos el ID de la URL si viene, para validaciones extra si quisieras
            if (int.TryParse(Request.Query["id"], out int id))
            {
                ViewBag.ActivoId = id;
            }
            return View();
        }

        // Vista para imprimir (Layout = null)
        public IActionResult ActaImpresion(int id)
        {
            // Ahora recibimos el ID del MOVIMIENTO (Cabecera), no de la asignación detalle
            ViewBag.IdMovimiento = id;
            return View();
        }
        #endregion

        #region API: LISTADO Y CONSULTAS (Mejorado)

        [HttpGet]
        public async Task<JsonResult> GetActivos(int? empresaIdFiltro)
        {
            try
            {
                var query = _context.Activos.AsQueryable();

                if (EsAdminGlobal)
                {
                    if (empresaIdFiltro.HasValue && empresaIdFiltro.Value > 0)
                        query = query.Where(a => a.EmpresaId == empresaIdFiltro.Value);
                }
                else
                {
                    query = query.Where(a => a.EmpresaId == EmpresaUsuarioId);
                }

                var data = await (from a in query
                                  join t in _context.ActivoTipos on a.TipoId equals t.Id into tj
                                  from t in tj.DefaultIfEmpty()
                                  join m in _context.Marcas on a.MarcaId equals m.Id into mj
                                  from m in mj.DefaultIfEmpty()
                                  join mo in _context.Modelos on a.ModeloId equals mo.Id into moj
                                  from mo in moj.DefaultIfEmpty()

                                      // Subquery para estado actual
                                  let ultimoMov = (from d in _context.DMovimientosActivo
                                                   join mov in _context.MovimientosActivo on d.MovimientoId equals mov.Id
                                                   where d.ActivoId == a.Id && mov.Estado == true
                                                   orderby mov.FechaMovimiento descending, mov.Id descending
                                                   select new { mov.PersonalId, mov.UbicacionDestino }).FirstOrDefault()

                                  join p in _context.Personal on ultimoMov.PersonalId equals p.Id into pj
                                  from p in pj.DefaultIfEmpty()

                                  select new
                                  {
                                      Id = a.Id,
                                      Codigo = a.CodigoInterno ?? "S/C",
                                      Descripcion = (t.Nombre ?? "") + " " + (m.Nombre ?? "") + " " + (mo.Nombre ?? ""),
                                      Serie = a.Serie ?? "-",
                                      Condicion = a.Condicion,
                                      Situacion = a.Situacion,

                                      // Visual
                                      AsignadoA = (a.Situacion == "EN STOCK") ? " EN STOCK" : (p != null) ? p.NombresCompletos : "SIN ASIGNAR",

                                      // DATO CLAVE AGREGADO: ID DEL RESPONSABLE
                                      PersonalId = (p != null) ? p.Id : (int?)null,

                                      Ubicacion = ultimoMov != null ? ultimoMov.UbicacionDestino : "-"
                                  })
                                  .ToListAsync();

                var listadoOrdenado = data.OrderBy(x => x.AsignadoA).ToList();

                return Json(new { status = true, data = listadoOrdenado });
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
                return Json(new { status = true, data = activo, specs = specs });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetUbicaciones()
        {
            // Select Distinct para el combo de ubicaciones
            var ubicaciones = await _context.MovimientosActivo
                                            .Where(x => !string.IsNullOrEmpty(x.UbicacionDestino))
                                            .Select(x => x.UbicacionDestino)
                                            .Distinct()
                                            .OrderBy(x => x)
                                            .ToListAsync();
            return Json(new { status = true, data = ubicaciones });
        }

        #endregion

        #region API: REGISTRO ACTIVO (Igual que antes)

        public class ActivoRegistroModel
        {
            public int? GrupoId { get; set; }
            public int? TipoId { get; set; }
            public int? MarcaId { get; set; }
            public int? ModeloId { get; set; }
            public string Serie { get; set; }
            public string Condicion { get; set; }
            public List<ActivoEspecificacion> Specs { get; set; }
        }

        [HttpPost]
        public async Task<JsonResult> GuardarActivo(ActivoRegistroModel model)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var activo = new Activo
                    {
                        CodigoInterno = "ACT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        GrupoId = model.GrupoId,
                        TipoId = model.TipoId,
                        MarcaId = model.MarcaId,
                        ModeloId = model.ModeloId,
                        Serie = model.Serie,
                        Condicion = model.Condicion ?? "OPERATIVO",
                        Situacion = "EN STOCK",
                        EmpresaId = EmpresaUsuarioId,
                        FechaRegistro = DateTime.Now,
                        Estado = true
                    };

                    _context.Activos.Add(activo);
                    await _context.SaveChangesAsync();

                    if (model.Specs != null)
                    {
                        foreach (var s in model.Specs) { s.ActivoId = activo.Id; _context.ActivoEspecificaciones.Add(s); }
                        await _context.SaveChangesAsync();
                    }

                    // Movimiento Inicial (Alta a Stock) -> Usando la NUEVA TABLA
                    var mov = new MovimientoActivo
                    {
                        CodigoActa = "ALTA-" + activo.Id,
                        TipoMovimiento = "ALTA",
                        FechaMovimiento = DateTime.Now,
                        EmpresaId = EmpresaUsuarioId,
                        UsuarioRegistroId = UsuarioActualId,
                        UbicacionDestino = "STOCK",
                        Observacion = "INGRESO INICIAL AL SISTEMA",
                        Estado = true
                    };
                    _context.MovimientosActivo.Add(mov);
                    await _context.SaveChangesAsync();

                    var det = new DMovimientoActivo
                    {
                        MovimientoId = mov.Id,
                        ActivoId = activo.Id,
                        CondicionItem = activo.Condicion,
                        ObservacionItem = "Nuevo"
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

        #region API: GESTIÓN DE MOVIMIENTOS (EL CAMBIO GRANDE)

        // DTO para recibir el movimiento complejo (Cabecera + Lista de Items)
        public class MovimientoDTO
        {
            public string TipoMovimiento { get; set; } // 'ENTREGA' o 'DEVOLUCION'
            public int? PersonalId { get; set; } // Receptor (si es Entrega) o null (si es Devolución a stock)
            public string Ubicacion { get; set; }
            public string Observacion { get; set; }

            // Lista de activos seleccionados
            public List<ItemMovimientoDTO> Items { get; set; }
        }

        public class ItemMovimientoDTO
        {
            public int ActivoId { get; set; }
            public string Condicion { get; set; } // Importante para la devolución
            public string Observacion { get; set; }
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

                    // LÓGICA DE DETECCIÓN AUTOMÁTICA PARA DEVOLUCIONES
                    if (data.TipoMovimiento == "DEVOLUCION")
                    {
                        // Tomamos el primer activo del carrito para saber quién lo devuelve
                        // (Se asume que en un mismo acta de devolución, todos los activos vienen de la misma persona)
                        int primerActivoId = data.Items.First().ActivoId;

                        // Buscamos el último movimiento vigente de este activo para hallar al responsable
                        var ultimoMov = await (from d in _context.DMovimientosActivo
                                               join m in _context.MovimientosActivo on d.MovimientoId equals m.Id
                                               where d.ActivoId == primerActivoId && m.Estado == true
                                               orderby m.FechaMovimiento descending, m.Id descending
                                               select m.PersonalId).FirstOrDefaultAsync();

                        responsableIdDetectado = ultimoMov;

                        if (responsableIdDetectado == null)
                        {
                            return Json(new { status = false, message = "No se pudo identificar al responsable actual de los activos para procesar la devolución." });
                        }
                    }

                    // 1. Crear Cabecera del Acta
                    var cabecera = new MovimientoActivo
                    {
                        CodigoActa = "ACT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                        TipoMovimiento = data.TipoMovimiento,
                        FechaMovimiento = DateTime.Now,
                        EmpresaId = EmpresaUsuarioId,
                        UsuarioRegistroId = UsuarioActualId,

                        // Si es ENTREGA: usa el PersonalId que vino del combo.
                        // Si es DEVOLUCION: usa el responsableIdDetectado que buscamos arriba.
                        PersonalId = responsableIdDetectado,

                        UbicacionDestino = data.Ubicacion,
                        Observacion = data.Observacion,
                        Estado = true
                    };

                    _context.MovimientosActivo.Add(cabecera);
                    await _context.SaveChangesAsync();

                    // 2. Procesar cada ítem
                    foreach (var item in data.Items)
                    {
                        // A. Detalle del Movimiento
                        var detalle = new DMovimientoActivo
                        {
                            MovimientoId = cabecera.Id,
                            ActivoId = item.ActivoId,
                            CondicionItem = item.Condicion,
                            ObservacionItem = item.Observacion
                        };
                        _context.DMovimientosActivo.Add(detalle);

                        // B. Actualizar Maestro de Activo
                        var activoDb = await _context.Activos.FindAsync(item.ActivoId);
                        if (activoDb != null)
                        {
                            activoDb.Condicion = item.Condicion;

                            if (data.TipoMovimiento == "ENTREGA")
                            {
                                activoDb.Situacion = "EN USO";
                                if (responsableIdDetectado.HasValue)
                                {
                                    var per = await _context.Personal.FindAsync(responsableIdDetectado);
                                    if (per != null && per.EmpresaId.HasValue) activoDb.EmpresaId = per.EmpresaId;
                                }
                            }
                            else // DEVOLUCION
                            {
                                activoDb.Situacion = "EN STOCK";
                                // Al devolver, el activo vuelve a la empresa del usuario que recibe (TI)
                                // activoDb.EmpresaId = EmpresaUsuarioId;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    tx.Commit();

                    return Json(new { status = true, message = "Movimiento registrado con éxito.", idMovimiento = cabecera.Id });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Json(new { status = false, message = "Error: " + ex.Message });
                }
            }
        }

        #endregion

        #region API: HISTORIAL E IMPRESIÓN (Adaptado a Grupos)

        [HttpGet]
        public async Task<JsonResult> GetHistorial(int activoId)
        {
            try
            {
                // 1. Info del activo para la cabecera del historial
                var activoHeader = await (from a in _context.Activos
                                          join t in _context.ActivoTipos on a.TipoId equals t.Id into tj
                                          from t in tj.DefaultIfEmpty()
                                          join m in _context.Marcas on a.MarcaId equals m.Id into mj
                                          from m in mj.DefaultIfEmpty()
                                          join mo in _context.Modelos on a.ModeloId equals mo.Id into moj
                                          from mo in moj.DefaultIfEmpty()
                                          where a.Id == activoId
                                          select new
                                          {
                                              Codigo = a.CodigoInterno,
                                              Serie = a.Serie,
                                              FullDesc = (t.Nombre ?? "") + " " + (m.Nombre ?? "") + " " + (mo.Nombre ?? "")
                                          }).FirstOrDefaultAsync();

                // 2. Movimientos: Listamos los DMOVIMIENTOS donde el ID coincide con el ACTIVO
                var historial = await (from d in _context.DMovimientosActivo
                                       join m in _context.MovimientosActivo on d.MovimientoId equals m.Id
                                       join p in _context.Personal on m.PersonalId equals p.Id into pj
                                       from p in pj.DefaultIfEmpty()
                                       where d.ActivoId == activoId // Filtro estricto por el activo actual
                                       orderby m.FechaMovimiento descending
                                       select new
                                       {
                                           m.Id,
                                           Fecha = m.FechaMovimiento.Value.ToString("dd/MM/yyyy HH:mm"),
                                           Tipo = m.TipoMovimiento,
                                           Responsable = (m.TipoMovimiento == "DEVOLUCION") ? "ALMACÉN (REINGRESO)" : (p != null ? p.NombresCompletos : "STOCK"),
                                           Ubicacion = m.UbicacionDestino,
                                           Observacion = d.ObservacionItem, // Usamos la observación del ítem, no la general del acta
                                           RutaActa = m.RutaActaPdf,
                                           Estado = m.Estado == true ? "VIGENTE" : "ANULADO"
                                       }).ToListAsync();

                return Json(new { status = true, data = historial, activoInfo = activoHeader });
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

                // 1. Datos Generales y Recuperación de Items (Detalle)
                bool esDevolucion = movimiento.TipoMovimiento == "DEVOLUCION";
                string tipoActa = esDevolucion ? "ACTA DE DEVOLUCIÓN" : "ACTA DE ENTREGA";

                var items = await (from d in _context.DMovimientosActivo
                                   join a in _context.Activos on d.ActivoId equals a.Id
                                   join t in _context.ActivoTipos on a.TipoId equals t.Id into tj
                                   from t in tj.DefaultIfEmpty()
                                   join m in _context.Marcas on a.MarcaId equals m.Id into mj
                                   from m in mj.DefaultIfEmpty()
                                   join mo in _context.Modelos on a.ModeloId equals mo.Id into moj
                                   from mo in moj.DefaultIfEmpty()
                                   where d.MovimientoId == idMovimiento
                                   select new
                                   {
                                       a.Id,
                                       Tipo = t.Nombre,
                                       Marca = m.Nombre,
                                       Modelo = mo.Nombre,
                                       Serie = a.Serie,
                                       Condicion = d.CondicionItem,
                                       Specs = _context.ActivoEspecificaciones.Where(s => s.ActivoId == a.Id).ToList()
                                   }).ToListAsync();

                var listaItems = items.Select(i => new
                {
                    Equipo = $"{i.Tipo} {i.Marca}".Trim(),
                    Modelo = i.Modelo,
                    Serie = i.Serie,
                    Condicion = i.Condicion,
                    Caracteristicas = string.IsNullOrEmpty(string.Join(", ", i.Specs.Select(s => $"{s.Clave}: {s.Valor}"))) ? "Sin características" : string.Join(", ", i.Specs.Select(s => $"{s.Clave}: {s.Valor}"))
                }).ToList();

                // 2. Lógica de Personas y sus Empresas
                // Datos del Usuario del Sistema (TI/Administrador)
                var usuarioLogueado = await _context.Usuarios.FindAsync(movimiento.UsuarioRegistroId);
                var empresaLogueado = await _context.Empresas.FindAsync(movimiento.EmpresaId); // Empresa dueña del activo/movimiento

                string nombreUserSys = usuarioLogueado?.Nombre ?? "ADMINISTRADOR";
                string dniUserSys = usuarioLogueado?.Dni ?? "-";
                string empresaUserSys = empresaLogueado?.RazonSocial ?? "YNNOVACORP";

                // Variables para el Acta
                string nombreEmisor = "", dniEmisor = "", cargoEmisor = "", empresaEmisor = "";
                string nombreReceptor = "", dniReceptor = "", cargoReceptor = "", empresaReceptor = "";

                if (esDevolucion)
                {
                    // DEVOLUCIÓN: Emisor (Devuelve) = Personal | Receptor (Recibe) = Usuario TI
                    if (movimiento.PersonalId.HasValue)
                    {
                        var pDevuelve = await _context.Personal.FindAsync(movimiento.PersonalId);
                        var eDevuelve = await _context.Empresas.FindAsync(pDevuelve?.EmpresaId);

                        nombreEmisor = pDevuelve?.NombresCompletos;
                        dniEmisor = pDevuelve?.Dni;
                        cargoEmisor = pDevuelve?.Cargo;
                        empresaEmisor = eDevuelve?.RazonSocial ?? "-";
                    }

                    nombreReceptor = nombreUserSys;
                    dniReceptor = dniUserSys;
                    cargoReceptor = "LOGÍSTICA / TI";
                    empresaReceptor = empresaUserSys;
                }
                else
                {
                    // ENTREGA: Emisor (Entrega) = Usuario TI | Receptor (Recibe) = Personal
                    nombreEmisor = nombreUserSys;
                    dniEmisor = dniUserSys;
                    cargoEmisor = "LOGÍSTICA / TI";
                    empresaEmisor = empresaUserSys;

                    if (movimiento.PersonalId.HasValue)
                    {
                        var pRecibe = await _context.Personal.FindAsync(movimiento.PersonalId);
                        var eRecibe = await _context.Empresas.FindAsync(pRecibe?.EmpresaId);

                        nombreReceptor = pRecibe?.NombresCompletos;
                        dniReceptor = pRecibe?.Dni;
                        cargoReceptor = pRecibe?.Cargo;
                        empresaReceptor = eRecibe?.RazonSocial ?? "-";
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

                        // Objetos con Empresa incluida
                        Emisor = new
                        {
                            Nombre = nombreEmisor,
                            Dni = dniEmisor,
                            Cargo = cargoEmisor,
                            Empresa = empresaEmisor
                        },
                        Receptor = new
                        {
                            Nombre = nombreReceptor,
                            Dni = dniReceptor,
                            Cargo = cargoReceptor,
                            Empresa = empresaReceptor
                        },

                        Items = listaItems
                    }
                });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> SubirActa(int idMovimiento, IFormFile archivo)
        {
            // Guarda el PDF en la cabecera del movimiento
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

        #region COMBOS AUXILIARES
        [HttpGet]
        public JsonResult GetCombosRegistro()
        {
            return Json(new
            {
                status = true,
                grupos = _context.ActivoGrupos.Where(x => x.Estado == true).ToList(),
                tipos = _context.ActivoTipos.Where(x => x.Estado == true).ToList(),
                marcas = _context.Marcas.Where(x => x.Estado == true).ToList(),
                modelos = _context.Modelos.Where(x => x.Estado == true).ToList()
            });
        }

        [HttpGet]
        public JsonResult GetPersonalCombo()
        {
            var p = _context.Personal.Where(x => x.Estado == true && x.EmpresaId == EmpresaUsuarioId)
                            .Select(x => new { x.Id, x.NombresCompletos })
                            .OrderBy(x => x.NombresCompletos).ToList();
            return Json(new { status = true, data = p });
        }

        [HttpGet]
        public JsonResult GetEmpresasCombo()
        {
            // Solo para Admin Global
            if (!EsAdminGlobal) return Json(new { status = false });
            var e = _context.Empresas.Where(x => x.Estado == true).Select(x => new { x.Id, x.RazonSocial }).ToList();
            return Json(new { status = true, data = e });
        }
        #endregion
    }
}