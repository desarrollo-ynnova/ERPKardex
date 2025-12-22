using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPKardex.Controllers
{
    public class IngresoSalidaAlmController : Controller
    {
        private readonly ApplicationDbContext _context;
        public IngresoSalidaAlmController(ApplicationDbContext context) { _context = context; }

        public IActionResult Registrar() => View();

        #region APIs Maestro-Detalle
        [HttpPost]
        public JsonResult GuardarMovimiento(IngresoSalidaAlm cabecera, string detallesJson)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    cabecera.FechaRegistro = DateTime.Now;
                    _context.IngresoSalidaAlms.Add(cabecera);
                    _context.SaveChanges();

                    if (!string.IsNullOrEmpty(detallesJson))
                    {
                        var listaDetalles = JsonConvert.DeserializeObject<List<DIngresoSalidaAlm>>(detallesJson);
                        foreach (var detalle in listaDetalles)
                        {
                            detalle.Id = 0;
                            detalle.IngresoSalidaAlmId = cabecera.Id;
                            detalle.FechaRegistro = DateTime.Now;
                            _context.DIngresoSalidaAlms.Add(detalle);
                        }
                        _context.SaveChanges();
                    }
                    transaction.Commit();
                    return Json(new { status = true, message = "Movimiento registrado correctamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            }
        }
        #endregion

        #region APIs Combos Cabecera
        [HttpGet]
        public JsonResult GetEmpresas() => Json(new { data = _context.Empresas.ToList(), status = true });

        [HttpGet]
        public JsonResult GetSucursalesByEmpresa(int empresaId) =>
            Json(new { data = _context.Sucursales.Where(s => s.EmpresaId == empresaId && s.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetAlmacenesBySucursal(string codSucursal, int empresaId) =>
            Json(new { data = _context.Almacenes.Where(a => a.CodSucursal == codSucursal && a.EmpresaId == empresaId && a.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMotivosData() => Json(new { data = _context.Motivos.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetMonedaData() => Json(new { data = _context.Monedas.Where(m => m.Estado == true).ToList(), status = true });

        [HttpGet]
        public JsonResult GetTipoDocumentoData() => Json(new { data = _context.TipoDocumentos.Where(t => t.Estado == true).ToList(), status = true });
        #endregion

        #region APIs Filtrado de Productos (Cascada)
        [HttpGet]
        public JsonResult GetGruposData() => Json(new { data = _context.Grupos.ToList(), status = true });

        [HttpGet]
        public JsonResult GetSubgruposByGrupo(string codGrupo) =>
            Json(new { data = _context.Subgrupos.Where(s => s.CodGrupo == codGrupo).ToList(), status = true });

        [HttpGet]
        public JsonResult GetProductosBySubgrupo(string codSubgrupo) =>
            Json(new { data = _context.Productos.Where(p => p.CodSubgrupo == codSubgrupo).Select(p => new { p.Codigo, p.DescripcionProducto, p.CodUnidadMedida }).ToList(), status = true });
        #endregion
    }
}