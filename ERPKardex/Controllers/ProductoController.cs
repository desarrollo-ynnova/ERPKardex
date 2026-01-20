using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    // Heredamos de BaseController para acceder a EmpresaUsuarioId y UsuarioActualId
    public class ProductoController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProductoController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region VISTAS
        public IActionResult RegistroServicio() => View();
        public IActionResult Index() => View();
        public IActionResult Registrar() => View();
        public IActionResult Editar(string id) => View();
        public IActionResult Ver(string id) => View();

        // Vistas Parciales
        public IActionResult ObtenerVistaRegistroGrupo() => PartialView("_RegistrarGrupo");
        public IActionResult ObtenerVistaRegistroSubgrupo() => PartialView("_RegistrarSubgrupo");
        public IActionResult ObtenerVistaRegistroUnidadMedida() => PartialView("_RegistrarUnidadMedida");
        public IActionResult ObtenerVistaRegistroFormulacion() => PartialView("_RegistrarFormulacion");
        public IActionResult ObtenerVistaRegistroPeligrosidad() => PartialView("_RegistrarPeligrosidad");
        public IActionResult ObtenerVistaRegistroEmpresa() => PartialView("_RegistrarEmpresa");
        public IActionResult ObtenerVistaRegistroMarca() => PartialView("_RegistrarMarca");
        public IActionResult ObtenerVistaRegistroModelo() => PartialView("_RegistrarModelo");
        public IActionResult ObtenerVistaRegistroCuenta() => PartialView("_RegistrarCuenta");
        public IActionResult ObtenerVistaRegistroIA() => PartialView("_RegistrarIngredienteActivo");
        #endregion

        #region APIs

        [HttpGet]
        public JsonResult GetSucursalesByEmpresa(int empresaId) =>
            Json(new { data = _context.Sucursales.Where(s => s.EmpresaId == empresaId && s.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetAlmacenesBySucursal(int sucursalId, int empresaId) =>
            Json(new { data = _context.Almacenes.Where(a => a.SucursalId == sucursalId && a.EmpresaId == empresaId && a.Estado == true).ToList(), status = true });

        // GET PRODUCTOS
        [HttpGet]
        public JsonResult GetProductosData(int? almacenId)
        {
            try
            {
                // Uso de EmpresaUsuarioId heredado
                var productosData = (from pro in _context.Productos
                                     join disa in _context.DIngresoSalidaAlms on pro.Codigo equals disa.CodProducto
                                     join isa in _context.IngresoSalidaAlms on disa.IngresoSalidaAlmId equals isa.Id
                                     where isa.AlmacenId == almacenId
                                     join td in _context.TipoDocumentos on isa.TipoDocumentoId equals td.Id into joinDoc
                                     from td in joinDoc.DefaultIfEmpty()
                                     join ent in _context.Proveedores on isa.ProveedorId equals ent.Id into joinEnt
                                     from ent in joinEnt.DefaultIfEmpty()
                                     join tdi in _context.TiposDocumentoIdentidad on ent.TipoDocumentoIdentidadId equals tdi.Id into joinTdi
                                     from tdi in joinTdi.DefaultIfEmpty()
                                     where pro.EmpresaId == EmpresaUsuarioId // <--- CAMBIO AQUÍ
                                     select new
                                     {
                                         pro.Codigo,
                                         pro.CodGrupo,
                                         pro.DescripcionGrupo,
                                         pro.DescripcionComercial,
                                         pro.CodSubgrupo,
                                         pro.DescripcionSubgrupo,
                                         pro.DescripcionProducto,
                                         pro.CodUnidadMedida,
                                         disa.Cantidad,
                                         Proveedor = ((tdi.Descripcion ?? "") + ": " + (ent.NumeroDocumento ?? "") + " - " + (ent.RazonSocial ?? "")) ?? "Sin Proveedor",
                                         TipoDocumento = td != null ? td.Descripcion : "S/D",
                                         Documento = (isa.SerieDocumento ?? "") + " - " + (isa.NumeroDocumento ?? ""),
                                     }).ToList();

                return Json(new { data = productosData, message = "Productos retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false }); // Ajuste simple en ApiResponse si no existe la clase
            }
        }

        // GET EMPRESAS
        public JsonResult GetEmpresaData()
        {
            try
            {
                var empresaData = _context.Empresas.ToList();
                return Json(new { data = empresaData, message = "Empresas retornadas exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        // GET CUENTAS
        public JsonResult GetCuentaData()
        {
            try
            {
                var data = _context.Cuentas.Where(c => c.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // GET GRUPOS
        public JsonResult GetGrupos()
        {
            try
            {
                var data = _context.Grupos.Where(g => g.EmpresaId == EmpresaUsuarioId && !g.Codigo.StartsWith("6")).ToList(); // <--- CAMBIO AQUÍ
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // GET GRUPOS FILTRADOS POR CUENTA (Servicios)
        public JsonResult GetGruposServ()
        {
            try
            {
                var data = _context.Grupos.Where(g => g.EmpresaId == EmpresaUsuarioId && g.Codigo.StartsWith("6")).ToList(); // <--- CAMBIO AQUÍ
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // GET SUBGRUPOS
        public JsonResult GetSubgruposByGrupo(int grupoId)
        {
            try
            {
                var data = _context.Subgrupos.Where(s => s.GrupoId == grupoId && s.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
                return Json(new { data = data, status = true, message = "Subgrupos retornados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // Filtrar Modelos por Marca
        public JsonResult GetModelosByMarca(int marcaId)
        {
            try
            {
                var data = _context.Modelos.Where(m => m.MarcaId == marcaId && m.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
                return Json(new { data = data, status = true, message = "Modelos retornados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // GET UMEDIDA
        public JsonResult GetUMedidaData()
        {
            try
            {
                var umedidaData = _context.UnidadesMedida.ToList();
                return Json(new { data = umedidaData, message = "Unidades de medida retornadas exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        // GET FQ
        public JsonResult GetFQData()
        {
            try
            {
                var fqsData = _context.FormulacionesQuimicas.ToList();
                return Json(new { data = fqsData, message = "FQs retornados exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        // GET Peligrosidad
        public JsonResult GetPeligrosidadData()
        {
            try
            {
                var peligrosidadData = _context.Peligrosidades.ToList();
                return Json(new { data = peligrosidadData, message = "Peligrosidades retornadas exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        [HttpGet]
        public JsonResult GetMarcaData()
        {
            try
            {
                var marcas = _context.Marcas
                    .Where(m => m.EmpresaId == EmpresaUsuarioId) // <--- CAMBIO AQUÍ
                    .OrderBy(m => m.Nombre)
                    .ToList();

                return Json(new
                {
                    data = marcas,
                    message = "Marcas retornadas exitosamente.",
                    status = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetModeloData()
        {
            try
            {
                var modelos = _context.Modelos.Where(mo => mo.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
                return Json(new
                {
                    data = modelos,
                    message = "Modelos retornados exitosamente.",
                    status = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public JsonResult GetIngredienteActivoData()
        {
            try
            {
                var data = _context.IngredientesActivos.Where(i => i.EmpresaId == EmpresaUsuarioId).ToList(); // <--- CAMBIO AQUÍ
                return Json(new { data = data, status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        [HttpPost]
        public JsonResult RegistrarIngredienteActivo(IngredienteActivo ia)
        {
            try
            {
                ia.EmpresaId = EmpresaUsuarioId; // Aseguramos que se guarde con la empresa actual si el modelo lo requiere
                _context.IngredientesActivos.Add(ia);
                _context.SaveChanges();
                return Json(new { status = true, message = "Ingrediente registrado" });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        // POST Registrar empresa
        [HttpPost]
        public JsonResult RegistrarEmpresa(Empresa empresa)
        {
            try
            {
                if (_context.Empresas.Any(e => e.Ruc == empresa.Ruc))
                {
                    return Json(new { data = (object)null, message = "Ya existe una empresa registrada con el RUC ingresado.", status = false });
                }

                _context.Empresas.Add(empresa);
                _context.SaveChanges();
                return Json(new { data = (object)null, message = "Empresa registrada exitosamente.", status = true });
            }
            catch (Exception ex)
            {
                return Json(new { data = (object)null, message = ex.Message, status = false });
            }
        }

        // POST Registrar cuenta
        [HttpPost]
        public JsonResult RegistrarCuenta(Cuenta cuenta)
        {
            try
            {
                if (_context.Cuentas.Any(c => c.Codigo == cuenta.Codigo && c.EmpresaId == EmpresaUsuarioId)) // <--- CAMBIO AQUÍ
                {
                    return Json(new { status = false, message = "El código de cuenta ya existe." });
                }

                cuenta.EmpresaId = EmpresaUsuarioId; // Asignamos ID
                _context.Cuentas.Add(cuenta);
                _context.SaveChanges();
                return Json(new { status = true, message = "Cuenta registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        // POST Registrar Grupo
        [HttpPost]
        public JsonResult RegistrarGrupo(Grupo grupo)
        {
            try
            {
                if (_context.Grupos.Any(g => g.Codigo == grupo.Codigo && g.EmpresaId == EmpresaUsuarioId)) // <--- CAMBIO AQUÍ
                    return Json(new { status = false, message = "El código de grupo ya existe." });

                grupo.EmpresaId = EmpresaUsuarioId; // Asignamos ID
                _context.Grupos.Add(grupo);
                _context.SaveChanges();
                return Json(new { status = true, message = "Grupo registrado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarSubgrupo(Subgrupo subgrupo)
        {
            try
            {
                if (_context.Subgrupos.Any(s => s.Codigo == subgrupo.Codigo && s.CodGrupo == subgrupo.CodGrupo && s.EmpresaId == EmpresaUsuarioId)) // <--- CAMBIO AQUÍ
                {
                    return Json(new { status = false, message = "El código de subgrupo ya existe." });
                }

                subgrupo.CodGrupo = _context.Grupos.Where(g => g.Id == subgrupo.GrupoId).Select(g => g.Codigo).FirstOrDefault();
                subgrupo.DescripcionGrupo = subgrupo.DescripcionGrupo?.Split('-')[1].Trim();
                subgrupo.EmpresaId = EmpresaUsuarioId; // Asignamos ID

                _context.Subgrupos.Add(subgrupo);
                _context.SaveChanges();
                return Json(new { status = true, message = "Subgrupo registrado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarUnidadMedida(UnidadMedida unidad)
        {
            try
            {
                if (_context.UnidadesMedida.Any(u => u.Codigo == unidad.Codigo))
                {
                    return Json(new { status = false, message = "El código de unidad ya existe." });
                }

                _context.UnidadesMedida.Add(unidad);
                _context.SaveChanges();
                return Json(new { status = true, message = "Unidad de medida registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarFormulacion(FormulacionQuimica formulacion)
        {
            try
            {
                if (_context.FormulacionesQuimicas.Any(f => f.Codigo == formulacion.Codigo))
                {
                    return Json(new { status = false, message = "El código de formulación ya existe." });
                }

                _context.FormulacionesQuimicas.Add(formulacion);
                _context.SaveChanges();
                return Json(new { status = true, message = "Formulación registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarPeligrosidad(Peligrosidad peligrosidad)
        {
            try
            {
                if (_context.Peligrosidades.Any(p => p.Codigo == peligrosidad.Codigo))
                {
                    return Json(new { status = false, message = "El código de peligrosidad ya existe." });
                }

                _context.Peligrosidades.Add(peligrosidad);
                _context.SaveChanges();
                return Json(new { status = true, message = "Nivel de peligrosidad registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarMarca(Marca marca)
        {
            try
            {
                // Opcional: Validar si el nombre ya existe
                if (_context.Marcas.Any(m => m.Nombre == marca.Nombre && m.EmpresaId == EmpresaUsuarioId)) // <--- CAMBIO AQUÍ
                {
                    return Json(new { status = false, message = "Esta marca ya se encuentra registrada." });
                }

                marca.EmpresaId = EmpresaUsuarioId; // Asignamos ID
                _context.Marcas.Add(marca);
                _context.SaveChanges();
                return Json(new { status = true, message = "Marca registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarModelo(Modelo modelo)
        {
            try
            {
                // Validar si ya existe el modelo para esa marca específica
                if (_context.Modelos.Any(m => m.Nombre == modelo.Nombre && m.MarcaId == modelo.MarcaId && m.EmpresaId == EmpresaUsuarioId)) // <--- CAMBIO AQUÍ
                {
                    return Json(new { status = false, message = "Este modelo ya existe para la marca seleccionada." });
                }

                modelo.EmpresaId = EmpresaUsuarioId; // Asignamos ID
                _context.Modelos.Add(modelo);
                _context.SaveChanges();
                return Json(new { status = true, message = "Modelo registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarProductoCompleto(Producto producto, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    producto.CodGrupo = _context.Grupos.Where(g => g.Id == producto.GrupoId).Select(g => g.Codigo).FirstOrDefault();
                    producto.CodSubgrupo = _context.Subgrupos.Where(g => g.Id == producto.SubgrupoId).Select(g => g.Codigo).FirstOrDefault();
                    producto.DescripcionGrupo = producto.DescripcionGrupo?.Split('-')[1].Trim();
                    producto.DescripcionSubgrupo = producto.DescripcionSubgrupo?.Split('-')[1].Trim();
                    producto.EmpresaId = EmpresaUsuarioId; // <--- CAMBIO AQUÍ
                    producto.FechaRegistro = DateTime.Now;

                    // 1. Generación Automática del Código
                    string prefijo = $"{producto.CodSubgrupo}";

                    var ultimoProducto = _context.Productos
                        .Where(p => p.Codigo.StartsWith(prefijo))
                        .OrderByDescending(p => p.Codigo)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;

                    if (ultimoProducto != null)
                    {
                        string parteCorrelativa = ultimoProducto.Codigo.Substring(prefijo.Length);
                        if (int.TryParse(parteCorrelativa, out int ultimoCorrelativo))
                        {
                            nuevoCorrelativo = ultimoCorrelativo + 1;
                        }
                    }

                    producto.Codigo = $"{prefijo}{nuevoCorrelativo.ToString("D5")}";

                    if (_context.Productos.Any(p => p.Codigo == producto.Codigo))
                        return Json(new { status = false, message = $"Error crítico: El código generado {producto.Codigo} ya existe." });

                    // --- LÓGICA DE DESCRIPCIÓN COMERCIAL ---
                    if (string.IsNullOrEmpty(producto.DescripcionComercial))
                    {
                        // Caso A: Producto con Identificación Técnica
                        if (!string.IsNullOrEmpty(producto.DescripcionProducto) &&
                            producto.MarcaId != null &&
                            producto.ModeloId != null &&
                            !string.IsNullOrEmpty(producto.Serie))
                        {
                            var marcaNom = _context.Marcas.Find(producto.MarcaId)?.Nombre;
                            var modeloNom = _context.Modelos.Find(producto.ModeloId)?.Nombre;
                            producto.DescripcionComercial = $"{producto.DescripcionProducto} {marcaNom} {modeloNom} {producto.Serie}".ToUpper();
                        }
                        // Caso B: Producto Insumo/Químico
                        else if (!string.IsNullOrEmpty(producto.DescripcionProducto) &&
                                 producto.Concentracion != null &&
                                 !string.IsNullOrEmpty(producto.CodFormulacionQuimica))
                        {
                            var fqNom = _context.FormulacionesQuimicas.FirstOrDefault(f => f.Codigo == producto.CodFormulacionQuimica)?.Nombre;
                            producto.DescripcionComercial = $"{producto.DescripcionProducto} {producto.Concentracion} {fqNom}".ToUpper();
                        }
                        else
                        {
                            producto.DescripcionComercial = null;
                        }
                    }

                    // 3. Guardar el Producto
                    _context.Productos.Add(producto);
                    _context.SaveChanges();

                    // 4. Procesar Detalles (Ingredientes Activos)
                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DetalleIngredienteActivo>>(detallesJson);
                        foreach (var detalle in listaDetalles)
                        {
                            detalle.Id = 0;
                            detalle.CodProducto = producto.Codigo;
                            _context.DetallesIngredientesActivos.Add(detalle);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new
                    {
                        status = true,
                        message = $"Producto registrado exitosamente con el código: {producto.Codigo}",
                        codigoGenerado = producto.Codigo
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            }
        }

        [HttpPost]
        public JsonResult RegistrarServicio(Producto producto)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Completar datos del backend
                    producto.CodGrupo = _context.Grupos.Where(g => g.Id == producto.GrupoId).Select(g => g.Codigo).FirstOrDefault();
                    producto.CodSubgrupo = _context.Subgrupos.Where(g => g.Id == producto.SubgrupoId).Select(g => g.Codigo).FirstOrDefault();

                    // Limpieza de nombres
                    producto.DescripcionGrupo = producto.DescripcionGrupo?.Split('-').LastOrDefault()?.Trim();
                    producto.DescripcionSubgrupo = producto.DescripcionSubgrupo?.Split('-').LastOrDefault()?.Trim();

                    producto.EmpresaId = EmpresaUsuarioId; // <--- CAMBIO AQUÍ
                    producto.FechaRegistro = DateTime.Now;

                    // Para servicios, la descripción comercial suele ser igual a la descripción simple
                    producto.DescripcionComercial = producto.DescripcionProducto.ToUpper();

                    // --- 1. Generación Automática del Código ---
                    string prefijo = $"{producto.CodSubgrupo}";

                    var ultimoProducto = _context.Productos
                        .Where(p => p.Codigo.StartsWith(prefijo))
                        .OrderByDescending(p => p.Codigo)
                        .FirstOrDefault();

                    int nuevoCorrelativo = 1;
                    if (ultimoProducto != null)
                    {
                        string parteCorrelativa = ultimoProducto.Codigo.Substring(prefijo.Length);
                        if (int.TryParse(parteCorrelativa, out int ultimoCorrelativo))
                        {
                            nuevoCorrelativo = ultimoCorrelativo + 1;
                        }
                    }
                    producto.Codigo = $"{prefijo}{nuevoCorrelativo.ToString("D5")}";

                    // Validar duplicado
                    if (_context.Productos.Any(p => p.Codigo == producto.Codigo))
                        return Json(new { status = false, message = $"Error: El código generado {producto.Codigo} ya existe. Intente nuevamente." });

                    // --- 2. Guardar ---
                    _context.Productos.Add(producto);
                    _context.SaveChanges();

                    transaction.Commit();

                    return Json(new
                    {
                        status = true,
                        message = $"Servicio registrado correctamente: {producto.Codigo}"
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            }
        }
        #endregion
    }
}