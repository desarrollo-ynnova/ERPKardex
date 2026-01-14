using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Controllers
{
    public class TipoCambioController : BaseController // Asumo herencia de BaseController por el usuario
    {
        private readonly ApplicationDbContext _context;

        public TipoCambioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetHistorial()
        {
            try
            {
                // Traemos los últimos 30 días para no saturar, ordenado por fecha descendente
                var data = await _context.TipoCambios
                                         .OrderByDescending(x => x.Fecha)
                                         .Take(30)
                                         .ToListAsync();

                // Formateamos la fecha para la vista
                var result = data.Select(x => new
                {
                    x.Id,
                    Fecha = x.Fecha.ToString("dd/MM/yyyy"),
                    x.TcCompra,
                    x.TcVenta
                });

                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Guardar(DateTime fecha, decimal compra, decimal venta)
        {
            try
            {
                // Validación de Existencia
                var existe = await _context.TipoCambios.AnyAsync(x => x.Fecha.Date == fecha.Date);
                if (existe)
                {
                    return Json(new { status = false, message = $"Ya existe un Tipo de Cambio registrado para la fecha {fecha:dd/MM/yyyy}. No se permiten modificaciones." });
                }

                var nuevo = new TipoCambio
                {
                    Fecha = fecha,
                    TcCompra = compra,
                    TcVenta = venta,
                    Estado = true,
                    FechaRegistro = DateTime.Now
                };
                _context.TipoCambios.Add(nuevo);
                await _context.SaveChangesAsync();

                return Json(new { status = true, message = "Tipo de Cambio registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        // API CRÍTICA: Esta se usará desde Órdenes de Compra/Servicio/Pago
        [HttpGet]
        public async Task<JsonResult> GetTcPorFecha(DateTime fecha)
        {
            // Busca el TC exacto de esa fecha
            var tc = await _context.TipoCambios
                                   .Where(x => x.Fecha.Date == fecha.Date)
                                   .Select(x => x.TcVenta)
                                   .FirstOrDefaultAsync();

            if (tc > 0)
            {
                return Json(new { status = true, valor = tc });
            }
            else
            {
                // Opcional: Buscar el último registrado si no hay del día exacto
                var ultimo = await _context.TipoCambios
                                           .Where(x => x.Fecha < fecha)
                                           .OrderByDescending(x => x.Fecha)
                                           .Select(x => x.TcVenta)
                                           .FirstOrDefaultAsync();

                return Json(new { status = false, message = "No hay T.C. registrado para esta fecha.", ultimoConocido = ultimo });
            }
        }
    }
}