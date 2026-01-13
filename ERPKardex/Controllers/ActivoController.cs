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
        public IActionResult Index() => View();
        public IActionResult Registro() => View();
        public IActionResult Asignacion() => View();
        public IActionResult Historial() => View();

        // Vista para imprimir (Layout = null)
        public IActionResult ActaImpresion(int id)
        {
            ViewBag.IdAsignacion = id;
            return View();
        }
        #endregion

        #region API: LISTADO Y CONSULTAS

        [HttpGet]
        public async Task<JsonResult> GetActivos()
        {
            try
            {
                var query = _context.Activos.AsQueryable();

                if (!EsAdminGlobal)
                {
                    query = query.Where(a => a.EmpresaId == EmpresaUsuarioId);
                }

                var listado = await (from a in query
                                     join t in _context.ActivoTipos on a.TipoId equals t.Id into tj
                                     from t in tj.DefaultIfEmpty()
                                     join m in _context.Marcas on a.MarcaId equals m.Id into mj
                                     from m in mj.DefaultIfEmpty()
                                     join mo in _context.Modelos on a.ModeloId equals mo.Id into moj
                                     from mo in moj.DefaultIfEmpty()

                                         // Subquery para la asignación vigente
                                     let asignacion = _context.ActivoAsignaciones
                                                        .Where(x => x.ActivoId == a.Id && x.Estado == true)
                                                        .OrderByDescending(x => x.Id)
                                                        .FirstOrDefault()

                                     join p in _context.Personal on asignacion.PersonalId equals p.Id into pj
                                     from p in pj.DefaultIfEmpty()

                                     select new
                                     {
                                         Id = a.Id,
                                         Codigo = a.CodigoInterno ?? "S/C",
                                         Tipo = t.Nombre ?? "-",
                                         Marca = m.Nombre ?? "-",
                                         Modelo = mo.Nombre ?? "-",
                                         Equipo = (t.Nombre ?? "") + " " + (m.Nombre ?? "") + " " + (mo.Nombre ?? ""), // Concatenado para tabla
                                         Serie = a.Serie ?? "-",
                                         Condicion = a.Condicion, // Operativo, Malogrado
                                         Situacion = a.Situacion, // En Uso, En Stock
                                         AsignadoA = (asignacion != null && asignacion.EsStock == true) ? "EN STOCK" :
                                                     (p != null) ? p.NombresCompletos : "SIN ASIGNAR",
                                         Ubicacion = asignacion != null ? asignacion.UbicacionTexto : "-"
                                     }).ToListAsync();

                return Json(new { status = true, data = listado });
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

        #endregion

        #region API: REGISTRO (ALTA)

        // Modelo auxiliar para recibir el JSON limpio desde JS
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
                    // 1. Crear Cabecera
                    var activo = new Activo
                    {
                        CodigoInterno = "ACT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        GrupoId = model.GrupoId,
                        TipoId = model.TipoId,
                        MarcaId = model.MarcaId,
                        ModeloId = model.ModeloId,
                        Serie = model.Serie,
                        Condicion = model.Condicion ?? "OPERATIVO", // Default
                        Situacion = "EN STOCK", // Nace en Stock
                        EmpresaId = EmpresaUsuarioId,
                        FechaRegistro = DateTime.Now,
                        Estado = true
                    };

                    _context.Activos.Add(activo);
                    await _context.SaveChangesAsync();

                    // 2. Guardar Especificaciones
                    if (model.Specs != null && model.Specs.Count > 0)
                    {
                        foreach (var s in model.Specs)
                        {
                            s.ActivoId = activo.Id;
                            _context.ActivoEspecificaciones.Add(s);
                        }
                        await _context.SaveChangesAsync();
                    }

                    // 3. Asignación Inicial (Stock)
                    var asignacion = new ActivoAsignacion
                    {
                        ActivoId = activo.Id,
                        EsStock = true,
                        FechaAsignacion = DateTime.Now,
                        UbicacionTexto = "ALMACÉN CENTRAL",
                        Observacion = "ALTA DE ACTIVO",
                        Estado = true,
                        UsuarioRegistroId = UsuarioActualId
                    };
                    _context.ActivoAsignaciones.Add(asignacion);
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

        #region API: MOVIMIENTOS (ASIGNACIÓN Y DEVOLUCIÓN)

        [HttpGet]
        public async Task<JsonResult> GetDatosAsignacion(int activoId)
        {
            var activo = await _context.Activos.FindAsync(activoId);
            if (activo == null) return Json(new { status = false, message = "No existe" });

            // Buscar asignación actual VIGENTE
            var actual = await _context.ActivoAsignaciones
                                       .Where(x => x.ActivoId == activoId && x.Estado == true)
                                       .OrderByDescending(x => x.Id)
                                       .FirstOrDefaultAsync();

            string responsable = "ALMACÉN / STOCK";
            if (actual != null && actual.PersonalId != null)
            {
                var p = await _context.Personal.FindAsync(actual.PersonalId);
                responsable = p?.NombresCompletos ?? "DESCONOCIDO";
            }

            return Json(new
            {
                status = true,
                activo = new { activo.Id, activo.CodigoInterno, activo.Serie, activo.Condicion },
                actual = new { Responsable = responsable, Ubicacion = actual?.UbicacionTexto ?? "-" }
            });
        }

        // 1. ASIGNACIÓN (Stock -> Persona / Persona A -> Persona B)
        [HttpPost]
        public async Task<JsonResult> GuardarAsignacion(int activoId, int personalId, string ubicacion, string observacion)
        {
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    // Cerrar anterior
                    var anteriores = await _context.ActivoAsignaciones.Where(x => x.ActivoId == activoId && x.Estado == true).ToListAsync();
                    foreach (var a in anteriores) { a.Estado = false; a.FechaDevolucion = DateTime.Now; }

                    // Nueva Asignación
                    var nueva = new ActivoAsignacion
                    {
                        ActivoId = activoId,
                        PersonalId = personalId,
                        EsStock = false,
                        FechaAsignacion = DateTime.Now,
                        UbicacionTexto = ubicacion,
                        Observacion = observacion,
                        Estado = true,
                        UsuarioRegistroId = UsuarioActualId
                    };
                    _context.ActivoAsignaciones.Add(nueva);

                    // Actualizar Activo
                    var activo = await _context.Activos.FindAsync(activoId);
                    activo.Situacion = "EN USO";

                    // Actualizar empresa del activo a la del personal
                    var persona = await _context.Personal.FindAsync(personalId);
                    if (persona?.EmpresaId != null) activo.EmpresaId = persona.EmpresaId;

                    await _context.SaveChangesAsync();
                    tx.Commit();

                    return Json(new { status = true, message = "Asignación realizada", idAsignacion = nueva.Id });
                }
                catch (Exception ex) { tx.Rollback(); return Json(new { status = false, message = ex.Message }); }
            }
        }

        // 2. DEVOLUCIÓN (Persona -> Stock)
        [HttpPost]
        public async Task<JsonResult> GuardarDevolucion(int activoId, string condicionRetorno, string ubicacion, string observacion)
        {
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    // Cerrar anterior (El usuario que lo tenía)
                    var anteriores = await _context.ActivoAsignaciones.Where(x => x.ActivoId == activoId && x.Estado == true).ToListAsync();
                    foreach (var a in anteriores) { a.Estado = false; a.FechaDevolucion = DateTime.Now; }

                    // Nueva Asignación (A Stock)
                    var nueva = new ActivoAsignacion
                    {
                        ActivoId = activoId,
                        PersonalId = null, // Nadie
                        EsStock = true,    // Stock
                        FechaAsignacion = DateTime.Now,
                        UbicacionTexto = ubicacion, // Ej: Almacén TI
                        Observacion = "DEVOLUCIÓN: " + observacion,
                        Estado = true,
                        UsuarioRegistroId = UsuarioActualId
                    };
                    _context.ActivoAsignaciones.Add(nueva);

                    // Actualizar Activo
                    var activo = await _context.Activos.FindAsync(activoId);
                    activo.Situacion = "EN STOCK";
                    activo.Condicion = condicionRetorno; // Actualizamos si viene roto o bueno

                    await _context.SaveChangesAsync();
                    tx.Commit();

                    return Json(new { status = true, message = "Devolución registrada", idAsignacion = nueva.Id });
                }
                catch (Exception ex) { tx.Rollback(); return Json(new { status = false, message = ex.Message }); }
            }
        }

        #endregion

        #region API: DATOS PARA IMPRIMIR ACTA (Inteligente)

        [HttpGet]
        public async Task<JsonResult> GetDatosActa(int idAsignacion)
        {
            try
            {
                var movimiento = await _context.ActivoAsignaciones.FindAsync(idAsignacion);
                if (movimiento == null) return Json(new { status = false, message = "Movimiento no encontrado" });

                var activo = await _context.Activos.FindAsync(movimiento.ActivoId);

                // --- Determinar Tipo de Acta ---
                bool esDevolucion = (movimiento.EsStock == true);
                string tipoActa = esDevolucion ? "ACTA DE DEVOLUCIÓN" : "ACTA DE ENTREGA";

                // --- Obtener Personas (Emisor y Receptor) ---
                string nombreEmisor = "", dniEmisor = "", cargoEmisor = "", empresaEmisor = "";
                string nombreReceptor = "", dniReceptor = "", cargoReceptor = "", ubicacion = "";

                // Usuario del Sistema (Generalmente IT/Logística - Quien está logueado haciendo la operación)
                var usuarioLogueado = await _context.Usuarios.FindAsync(UsuarioActualId);
                string nombreUserSys = usuarioLogueado?.Nombre ?? "ADMINISTRADOR SISTEMA";
                string dniUserSys = usuarioLogueado?.Dni ?? "-"; // Recuperamos el DNI de la tabla usuario

                // Empresa del Sistema (Hardcodeada o recuperada)
                string empresaSys = "YNNOVACORP";

                if (esDevolucion)
                {
                    // CASO DEVOLUCIÓN:
                    // EMISOR (Quien devuelve): El usuario que tenía el activo ANTES.
                    // RECEPTOR (Quien recibe): El usuario del sistema (IT/Logística).

                    // Buscamos el movimiento INMEDIATAMENTE ANTERIOR a este
                    var movAnterior = await _context.ActivoAsignaciones
                                                    .Where(x => x.ActivoId == movimiento.ActivoId && x.Id < movimiento.Id)
                                                    .OrderByDescending(x => x.Id)
                                                    .FirstOrDefaultAsync();

                    if (movAnterior != null && movAnterior.PersonalId != null)
                    {
                        var pDevuelve = await _context.Personal.FindAsync(movAnterior.PersonalId);
                        // Opcional: recuperar empresa del personal si es necesario
                        var empDevuelve = await _context.Empresas.FindAsync(pDevuelve.EmpresaId);

                        nombreEmisor = pDevuelve.NombresCompletos;
                        dniEmisor = pDevuelve.Dni;
                        cargoEmisor = pDevuelve.Cargo;
                        empresaEmisor = empDevuelve?.RazonSocial ?? "-";
                    }
                    else
                    {
                        nombreEmisor = "USUARIO DESCONOCIDO";
                        dniEmisor = "-";
                    }

                    nombreReceptor = nombreUserSys; // IT Recibe
                    dniReceptor = dniUserSys;
                    cargoReceptor = "SOPORTE TI / LOGÍSTICA";
                    ubicacion = movimiento.UbicacionTexto;
                }
                else
                {
                    // CASO ENTREGA:
                    // EMISOR (Quien entrega): El usuario del sistema (IT).
                    // RECEPTOR (Quien recibe): El personal seleccionado.

                    nombreEmisor = nombreUserSys; // IT Entrega
                    dniEmisor = dniUserSys;
                    cargoEmisor = "EQUIPO DE TI";
                    empresaEmisor = empresaSys;

                    var pRecibe = await _context.Personal.FindAsync(movimiento.PersonalId);
                    nombreReceptor = pRecibe?.NombresCompletos ?? "EXTERNO";
                    cargoReceptor = pRecibe?.Cargo ?? "-";
                    dniReceptor = pRecibe?.Dni ?? "-";
                    ubicacion = movimiento.UbicacionTexto;
                }

                // --- Construir Características ---
                var marca = await _context.Marcas.FindAsync(activo.MarcaId);
                var modelo = await _context.Modelos.FindAsync(activo.ModeloId);
                var tipo = await _context.ActivoTipos.FindAsync(activo.TipoId);
                var specs = await _context.ActivoEspecificaciones.Where(x => x.ActivoId == activo.Id).ToListAsync();

                List<string> listaSpecs = new List<string>();
                if (modelo != null) listaSpecs.Add($"Modelo: {modelo.Nombre}");
                if (!string.IsNullOrEmpty(activo.Serie)) listaSpecs.Add($"S/N: {activo.Serie}");

                foreach (var s in specs)
                {
                    listaSpecs.Add($"{s.Clave}: {s.Valor}");
                }

                string caracteristicasTexto = string.Join(", ", listaSpecs);

                // --- Retorno Final ---
                return Json(new
                {
                    status = true,
                    data = new
                    {
                        Titulo = tipoActa,
                        Fecha = movimiento.FechaAsignacion?.ToString("dd/MM/yyyy"),

                        // Cabecera Acta
                        EmisorNombre = nombreEmisor,
                        EmisorDni = dniEmisor, // ¡Aquí va el DNI corregido!
                        EmisorCargo = cargoEmisor,
                        EmisorEmpresa = empresaEmisor,

                        ReceptorNombre = nombreReceptor,
                        ReceptorDni = dniReceptor,
                        ReceptorCargo = cargoReceptor,

                        // Tabla
                        EquipoTipo = tipo?.Nombre ?? "EQUIPO",
                        EquipoMarca = marca?.Nombre ?? "",
                        Caracteristicas = caracteristicasTexto,
                        Cantidad = "1",

                        // Firmas (Para poner debajo de la línea)
                        FirmaEntrega = nombreEmisor,
                        FirmaEntregaDni = dniEmisor, // DNI para la firma izquierda

                        FirmaRecibe = nombreReceptor,
                        FirmaRecibeDni = dniReceptor // DNI para la firma derecha
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region API: HISTORIAL Y SUBIDA
        [HttpGet]
        public async Task<JsonResult> GetHistorial(int activoId)
        {
            var hist = await (from h in _context.ActivoAsignaciones
                              join p in _context.Personal on h.PersonalId equals p.Id into pj
                              from p in pj.DefaultIfEmpty()
                              where h.ActivoId == activoId
                              orderby h.FechaAsignacion descending
                              select new
                              {
                                  h.Id,
                                  Fecha = h.FechaAsignacion.Value.ToString("dd/MM/yyyy HH:mm"),
                                  Tipo = (h.EsStock == true) ? "EN STOCK" : "ASIGNADO",
                                  Responsable = (h.EsStock == true) ? "ALMACÉN" : p.NombresCompletos,
                                  Ubicacion = h.UbicacionTexto,
                                  Observacion = h.Observacion,
                                  RutaActa = h.RutaActa,
                                  Estado = h.Estado == true ? "VIGENTE" : "HISTÓRICO"
                              }).ToListAsync();

            return Json(new { status = true, data = hist });
        }

        [HttpPost]
        public async Task<JsonResult> SubirActa(int idAsignacion, IFormFile archivo)
        {
            try
            {
                if (archivo == null) return Json(new { status = false, message = "Sin archivo" });
                var asig = await _context.ActivoAsignaciones.FindAsync(idAsignacion);

                var path = Path.Combine(_env.WebRootPath, "uploads", "actas");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string fileName = $"ACTA_{asig.Id}_{DateTime.Now.Ticks}.pdf";
                using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                asig.RutaActa = "uploads/actas/" + fileName;
                await _context.SaveChangesAsync();

                return Json(new { status = true, message = "Archivo subido" });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region COMBOS
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
            var p = _context.Personal
                            .Where(x => x.Estado == true && x.EmpresaId == EmpresaUsuarioId)
                            .Select(x => new { x.Id, x.NombresCompletos })
                            .OrderBy(x => x.NombresCompletos)
                            .ToList();
            return Json(new { status = true, data = p });
        }
        #endregion
    }
}