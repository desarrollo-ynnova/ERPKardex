using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class EntidadController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EntidadController(ApplicationDbContext context)
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
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

                // NOTA: Quitamos el filtro 'Estado == true' para ver también los inactivos y poder reactivarlos
                var data = await _context.Entidades
                    .Where(e => e.EmpresaId == empresaId)
                    .OrderByDescending(e => e.Id)
                    .Select(e => new
                    {
                        e.Id,
                        e.Ruc,
                        e.RazonSocial,
                        e.Telefono,
                        e.Email,
                        e.Estado // Necesario para pintar el badge en la vista
                    })
                    .ToListAsync();

                return Json(new { status = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // 2. OBTENER UNO (Para cargar formulario)
        [HttpGet]
        public async Task<JsonResult> Obtener(int id)
        {
            try
            {
                var entidad = await _context.Entidades.FindAsync(id);
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
        public async Task<JsonResult> Guardar(Entidad modelo)
        {
            try
            {
                var empresaId = int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");
                modelo.RazonSocial = modelo.RazonSocial?.ToUpper();

                // Validación de RUC duplicado en la misma empresa
                var existeRuc = await _context.Entidades
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
                    _context.Entidades.Add(modelo);
                }
                else
                {
                    // --- EDICIÓN ---
                    var entidadDb = await _context.Entidades.FindAsync(modelo.Id);
                    if (entidadDb == null) return Json(new { status = false, message = "No encontrado" });

                    // Actualizar datos
                    entidadDb.Ruc = modelo.Ruc;
                    entidadDb.RazonSocial = modelo.RazonSocial;
                    entidadDb.Telefono = modelo.Telefono;
                    entidadDb.Email = modelo.Email;

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
                var entidad = await _context.Entidades.FindAsync(id);
                if (entidad == null) return Json(new { status = false, message = "No encontrado" });

                // VALIDACIÓN DE INTEGRIDAD REFERENCIAL MANUAL
                // Consultamos si el ID aparece en alguna tabla transaccional
                bool tieneOrdenCompra = await _context.OrdenCompras.AnyAsync(x => x.EntidadId == id);
                bool tieneOrdenServicio = await _context.OrdenServicios.AnyAsync(x => x.EntidadId == id);
                bool tieneMovimientoAlmacen = await _context.IngresoSalidaAlms.AnyAsync(x => x.EntidadId == id);

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
                _context.Entidades.Remove(entidad);
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