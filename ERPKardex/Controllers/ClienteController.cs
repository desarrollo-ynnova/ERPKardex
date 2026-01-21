using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class ClienteController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ClienteController(ApplicationDbContext context)
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

                var query = from c in _context.Clientes
                            join td in _context.TiposDocumentoIdentidad on c.TipoDocumentoIdentidadId equals td.Id
                            // Left Joins para ubicación
                            join pa in _context.Paises on c.PaisId equals pa.Id into paJoin
                            from pa in paJoin.DefaultIfEmpty()
                            join ci in _context.Ciudades on c.CiudadId equals ci.Id into ciJoin
                            from ci in ciJoin.DefaultIfEmpty()

                            where c.Estado == true && c.EmpresaId == miEmpresaId
                            orderby c.Id descending
                            select new
                            {
                                c.Id,
                                // Ej: "RUC: 2055..."
                                Documento = td.Codigo + ": " + c.NumeroDocumento,
                                c.RazonSocial,
                                Ubicacion = (pa != null ? pa.Nombre : "") + (ci != null ? " - " + ci.Nombre : ""),
                                c.NombreContacto,
                                InfoContacto = (c.Telefono ?? "") + (string.IsNullOrEmpty(c.CorreoElectronico) ? "" : " / " + c.CorreoElectronico),
                                c.Estado
                            };

                return Json(new { status = true, data = await query.ToListAsync() });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion

        #region API MAESTROS
        [HttpGet]
        public async Task<JsonResult> GetCombosMaestros()
        {
            try
            {
                var tiposDoc = await _context.TiposDocumentoIdentidad.Where(x => x.Estado == true)
                    .Select(x => new { x.Id, x.Descripcion, x.Codigo, x.Longitud, x.EsAlfanumerico }).ToListAsync();

                var origenes = await _context.Origenes.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();
                var tiposPersona = await _context.TiposPersona.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();

                var paises = await _context.Paises.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre }).ToListAsync();
                var ciudades = await _context.Ciudades.Where(x => x.Estado == true).Select(x => new { x.Id, x.Nombre, x.PaisId }).ToListAsync();

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
            var ent = await _context.Clientes.FindAsync(id);
            return Json(new { status = ent != null, data = ent });
        }

        [HttpPost]
        public async Task<JsonResult> Guardar(Cliente modelo)
        {
            try
            {
                modelo.RazonSocial = modelo.RazonSocial?.ToUpper();
                modelo.NombreContacto = modelo.NombreContacto?.ToUpper();

                // Validación de duplicados
                var existe = await _context.Clientes.AnyAsync(x =>
                    x.TipoDocumentoIdentidadId == modelo.TipoDocumentoIdentidadId &&
                    x.NumeroDocumento == modelo.NumeroDocumento &&
                    x.EmpresaId == EmpresaUsuarioId &&
                    x.Id != modelo.Id);

                if (existe) return Json(new { status = false, message = "Ya existe un cliente con este documento." });

                if (modelo.Id == 0)
                {
                    modelo.EmpresaId = EmpresaUsuarioId;
                    modelo.FechaRegistro = DateTime.Now;
                    modelo.Estado = true;
                    _context.Clientes.Add(modelo);
                }
                else
                {
                    var db = await _context.Clientes.FindAsync(modelo.Id);
                    if (db == null) return Json(new { status = false, message = "No encontrado" });

                    // Mapeo completo
                    db.OrigenId = modelo.OrigenId;
                    db.TipoPersonaId = modelo.TipoPersonaId;
                    db.TipoDocumentoIdentidadId = modelo.TipoDocumentoIdentidadId;
                    db.NumeroDocumento = modelo.NumeroDocumento;
                    db.RazonSocial = modelo.RazonSocial;
                    db.Direccion = modelo.Direccion;
                    db.PaisId = modelo.PaisId;
                    db.CiudadId = modelo.CiudadId;

                    db.NombreContacto = modelo.NombreContacto;
                    db.CargoContacto = modelo.CargoContacto;
                    db.CorreoElectronico = modelo.CorreoElectronico;
                    db.Telefono = modelo.Telefono;

                    db.BancoId = modelo.BancoId;
                    db.CodigoSwift = modelo.CodigoSwift;

                    // Cuentas
                    db.MonedaIdUno = modelo.MonedaIdUno; db.NumeroCuentaUno = modelo.NumeroCuentaUno; db.NumeroCciUno = modelo.NumeroCciUno;
                    db.MonedaIdDos = modelo.MonedaIdDos; db.NumeroCuentaDos = modelo.NumeroCuentaDos; db.NumeroCciDos = modelo.NumeroCciDos;
                    db.MonedaIdTres = modelo.MonedaIdTres; db.NumeroCuentaTres = modelo.NumeroCuentaTres; db.NumeroCciTres = modelo.NumeroCciTres;

                    db.Estado = modelo.Estado;
                }

                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Cliente guardado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<JsonResult> Eliminar(int id)
        {
            try
            {
                var ent = await _context.Clientes.FindAsync(id);
                if (ent == null) return Json(new { status = false, message = "No encontrado" });

                // Validación Historial (Ejemplo con Ordenes Venta si tuvieras)
                /* bool tieneHistorial = await _context.OrdenVentas.AnyAsync(x => x.ClienteId == id);
                if (tieneHistorial) return Json(new { status = false, message = "Tiene historial. Délo de baja (Inactivo)." });
                */

                _context.Clientes.Remove(ent);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Eliminado correctamente." });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }
        #endregion
    }
}