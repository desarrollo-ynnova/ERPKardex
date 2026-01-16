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

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Registrar(int id = 0)
        {
            // Pasamos el ID a la vista para saber si es Nuevo o Edición
            ViewBag.Id = id;
            return View();
        }

        #endregion

        #region APIS JSON

        // 1. LISTAR TODOS (Activos e Inactivos)
        [HttpGet]
        public async Task<JsonResult> GetData()
        {
            try
            {
                var miEmpresaId = EmpresaUsuarioId;
                var esGlobal = EsAdminGlobal;

                // Hacemos Join con Banco para mostrar el nombre
                var query = from e in _context.Proveedores
                            join b in _context.Bancos on e.BancoId equals b.Id into bancoJoin
                            from b in bancoJoin.DefaultIfEmpty() // Left Join (puede no tener banco)
                            where e.Estado == true
                            select new
                            {
                                e.Id,
                                e.Ruc,
                                e.RazonSocial,
                                e.NombreContacto,
                                e.Telefono,
                                e.Email,
                                e.EmpresaId,
                                e.Estado,
                                // Datos Bancarios para mostrarlos o usarlos al editar
                                e.BancoId,
                                BancoNombre = b != null ? b.Nombre : "-",
                                e.NumeroCuenta,
                                e.NumeroCci,
                                e.NumeroDetraccion
                            };

                if (!esGlobal) query = query.Where(x => x.EmpresaId == miEmpresaId);

                var lista = await query.OrderByDescending(x => x.Id).ToListAsync();
                return Json(new { status = true, data = lista });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<JsonResult> GetBancosCombo()
        {
            try
            {
                var bancos = await _context.Bancos
                                           .Where(x => x.Estado == true)
                                           .Select(x => new { x.Id, x.Nombre })
                                           .ToListAsync();

                return Json(new { status = true, data = bancos });
            }
            catch (Exception ex) { return Json(new { status = false, message = ex.Message }); }
        }

        // 2. OBTENER UNO (Para cargar formulario)
        [HttpGet]
        public async Task<JsonResult> Obtener(int id)
        {
            try
            {
                var entidad = await _context.Proveedores.FindAsync(id);
                if (entidad == null) return Json(new { status = false, message = "No encontrado" });

                return Json(new { status = true, data = entidad });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // 3. GUARDAR (Creación y Edición con Cambio de Estado)
        [HttpPost]
        public async Task<JsonResult> Guardar(Proveedor modelo)
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                modelo.RazonSocial = modelo.RazonSocial?.ToUpper();
                modelo.NombreContacto = modelo.NombreContacto?.ToUpper();

                // Validación de RUC duplicado en la misma empresa
                var existeRuc = await _context.Proveedores
                    .AnyAsync(x => x.Ruc == modelo.Ruc
                                && x.EmpresaId == empresaId
                                && x.Id != modelo.Id); // Excluirse a sí mismo si edita

                if (existeRuc)
                    return Json(new { status = false, message = $"El RUC {modelo.Ruc} ya está registrado." });

                if (modelo.Id == 0)
                {
                    // --- NUEVO ---
                    modelo.EmpresaId = empresaId;
                    // Al crear, forzamos Activo (o respetamos lo que venga del front si quisieran crear inactivos)
                    modelo.Estado = true;
                    _context.Proveedores.Add(modelo);
                }
                else
                {
                    // --- EDICIÓN ---
                    var entidadDb = await _context.Proveedores.FindAsync(modelo.Id);
                    if (entidadDb == null) return Json(new { status = false, message = "No encontrado" });

                    // Actualizar datos
                    entidadDb.Ruc = modelo.Ruc;
                    entidadDb.RazonSocial = modelo.RazonSocial;
                    entidadDb.NombreContacto = modelo.NombreContacto;
                    entidadDb.Telefono = modelo.Telefono;
                    entidadDb.Email = modelo.Email;
                    entidadDb.NumeroCuenta = modelo.NumeroCuenta;
                    entidadDb.NumeroCci = modelo.NumeroCci;
                    entidadDb.NumeroDetraccion = modelo.NumeroDetraccion;
                    entidadDb.BancoId = modelo.BancoId;

                    // AQUÍ ES DONDE APLICAMOS LA BAJA O REACTIVACIÓN LÓGICA
                    // Si el usuario marcó Inactivo en el combo, aquí se guarda false.
                    entidadDb.Estado = modelo.Estado;
                }

                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "Datos guardados correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // 4. ELIMINAR ESTRICTO (Solo si no tiene historial)
        [HttpPost]
        public async Task<JsonResult> Eliminar(int id)
        {
            try
            {
                var entidad = await _context.Proveedores.FindAsync(id);
                if (entidad == null) return Json(new { status = false, message = "No encontrado" });

                // VALIDACIÓN DE INTEGRIDAD REFERENCIAL MANUAL
                // Consultamos si el ID aparece en alguna tabla transaccional
                bool tieneOrdenCompra = await _context.OrdenCompras.AnyAsync(x => x.ProveedorId == id);
                bool tieneOrdenServicio = await _context.OrdenServicios.AnyAsync(x => x.ProveedorId == id);
                bool tieneMovimientoAlmacen = await _context.IngresoSalidaAlms.AnyAsync(x => x.ProveedorId == id);

                if (tieneOrdenCompra || tieneOrdenServicio || tieneMovimientoAlmacen)
                {
                    // SI TIENE HISTORIAL, BLOQUEAMOS.
                    return Json(new
                    {
                        status = false,
                        message = "No se puede eliminar: Esta entidad tiene documentos asociados. Edite el registro y cambie el estado a 'INACTIVO' para darlo de baja."
                    });
                }

                // SI ESTÁ LIMPIO, BORRAMOS FÍSICAMENTE
                _context.Proveedores.Remove(entidad);
                await _context.SaveChangesAsync();

                return Json(new { status = true, message = "Registro eliminado físicamente de la base de datos." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error interno: " + ex.Message });
            }
        }

        #endregion
    }
}