using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class ProveedorController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProveedorController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region VISTAS
        public IActionResult Index() => View();

        public IActionResult Registrar(int id = 0)
        {
            ViewBag.Id = id;
            return View();
        }
        #endregion

        #region API LISTADO
        [HttpGet]
        public async Task<JsonResult> GetData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;

                var query = from p in _context.Proveedores
                                // Join con Tipo de Documento
                            join td in _context.TiposDocumentoIdentidad on p.TipoDocumentoIdentidadId equals td.Id
                            // Left Join con País
                            join pa in _context.Paises on p.PaisId equals pa.Id into paJoin
                            from pa in paJoin.DefaultIfEmpty()
                                // Left Join con Ciudad
                            join ci in _context.Ciudades on p.CiudadId equals ci.Id into ciJoin
                            from ci in ciJoin.DefaultIfEmpty()

                            where p.Estado == true && p.EmpresaId == miEmpresaId
                            orderby p.Id descending
                            select new
                            {
                                p.Id,
                                // Ej: "RUC: 20100..."
                                Documento = td.Codigo + ": " + p.NumeroDocumento,
                                p.RazonSocial,
                                // Ej: "PERÚ - LIMA"
                                Ubicacion = (pa != null ? pa.Nombre : "") + (ci != null ? " - " + ci.Nombre : ""),
                                p.NombreContacto,
                                // Concatenamos para ahorrar espacio en la tabla
                                InfoContacto = (p.Telefono ?? "") + (string.IsNullOrEmpty(p.CorreoElectronico) ? "" : " / " + p.CorreoElectronico),
                                p.Estado
                            };

                return Json(new { status = true, data = await query.ToListAsync() });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region API COMBOS AUXILIARES
        [HttpGet]
        public async Task<JsonResult> GetCombosMaestros()
        {
            try
            {
                // 1. Tipos de Documento (RUC, DNI, PASS, etc.)
                var tiposDoc = await _context.TiposDocumentoIdentidad
                    .Where(x => x.Estado == true)
                    .Select(x => new { x.Id, x.Descripcion, x.Codigo, x.Longitud, x.EsAlfanumerico })
                    .ToListAsync();

                // 2. Clasificaciones
                var origenes = await _context.Origenes.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();
                var tiposPersona = await _context.TiposPersona.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();

                // 3. Ubicación
                var paises = await _context.Paises.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();
                // Nota: Podrías cargar ciudades por demanda (evento change del país) si son muchas.
                var ciudades = await _context.Ciudades.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre, x.PaisId }).ToListAsync();

                // 4. Financiero
                var bancos = await _context.Bancos.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();
                var monedas = await _context.Monedas.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre, x.Simbolo }).ToListAsync();

                return Json(new { status = true, tiposDoc, origenes, tiposPersona, paises, ciudades, bancos, monedas });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region API CRUD
        [HttpGet]
        public async Task<JsonResult> Obtener(int id)
        {
            var ent = await _context.Proveedores.FindAsync(id);
            if (ent == null) return Json(new { status = false, message = "No encontrado" });
            return Json(new { status = true, data = ent });
        }

        [HttpPost]
        public async Task<JsonResult> Guardar(Proveedor modelo)
        {
            try
            {
                modelo.RazonSocial = modelo.RazonSocial?.ToUpper();
                modelo.NombreContacto = modelo.NombreContacto?.ToUpper();

                // Validación de Duplicidad (Unificada por Tipo + Número)
                var existe = await _context.Proveedores.AnyAsync(x =>
                    x.TipoDocumentoIdentidadId == modelo.TipoDocumentoIdentidadId &&
                    x.NumeroDocumento == modelo.NumeroDocumento &&
                    x.EmpresaId == EmpresaUsuarioId &&
                    x.Id != modelo.Id); // Ignorar el mismo registro al editar

                if (existe) return Json(new { status = false, message = "Ya existe un proveedor con este Número de Documento." });

                if (modelo.Id == 0)
                {
                    // --- NUEVO ---
                    modelo.EmpresaId = EmpresaUsuarioId;
                    modelo.FechaRegistro = DateTime.Now;
                    modelo.Estado = true; // Por defecto activo
                    _context.Proveedores.Add(modelo);
                }
                else
                {
                    // --- EDICIÓN ---
                    var db = await _context.Proveedores.FindAsync(modelo.Id);
                    if (db == null) return Json(new { status = false, message = "No encontrado" });

                    // 1. Clasificación e Identificación
                    db.OrigenId = modelo.OrigenId;
                    db.TipoPersonaId = modelo.TipoPersonaId;
                    db.TipoDocumentoIdentidadId = modelo.TipoDocumentoIdentidadId;
                    db.NumeroDocumento = modelo.NumeroDocumento;

                    // 2. Datos Generales
                    db.RazonSocial = modelo.RazonSocial;
                    db.Direccion = modelo.Direccion;
                    db.PaisId = modelo.PaisId;
                    db.CiudadId = modelo.CiudadId;

                    // 3. Contacto
                    db.NombreContacto = modelo.NombreContacto;
                    db.CargoContacto = modelo.CargoContacto;
                    db.CorreoElectronico = modelo.CorreoElectronico;
                    db.Telefono = modelo.Telefono;

                    // 4. Datos Bancarios
                    db.BancoId = modelo.BancoId;
                    db.CodigoSwift = modelo.CodigoSwift;

                    // Cuenta 1
                    db.MonedaIdUno = modelo.MonedaIdUno;
                    db.NumeroCuentaUno = modelo.NumeroCuentaUno;
                    db.NumeroCciUno = modelo.NumeroCciUno;

                    // Cuenta 2
                    db.MonedaIdDos = modelo.MonedaIdDos;
                    db.NumeroCuentaDos = modelo.NumeroCuentaDos;
                    db.NumeroCciDos = modelo.NumeroCciDos;

                    // Cuenta 3
                    db.MonedaIdTres = modelo.MonedaIdTres;
                    db.NumeroCuentaTres = modelo.NumeroCuentaTres;
                    db.NumeroCciTres = modelo.NumeroCciTres;

                    // 5. Estado
                    db.Estado = modelo.Estado;
                }

                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Proveedor guardado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = "Error: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> Eliminar(int id)
        {
            try
            {
                var entidad = await _context.Proveedores.FindAsync(id);
                if (entidad == null) return Json(new { status = false, message = "No encontrado" });

                // Validación de Historial
                bool tieneHistorial = await _context.OrdenCompras.AnyAsync(x => x.ProveedorId == id) ||
                                      await _context.OrdenServicios.AnyAsync(x => x.ProveedorId == id) ||
                                      await _context.IngresoSalidaAlms.AnyAsync(x => x.ProveedorId == id);

                if (tieneHistorial)
                {
                    return Json(new { status = false, message = "No se puede eliminar porque tiene historial. Cámbielo a INACTIVO." });
                }

                _context.Proveedores.Remove(entidad);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Eliminado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion
    }
}