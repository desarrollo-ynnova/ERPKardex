using ERPKardex.Data;
using ERPKardex.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERPKardex.Controllers
{
    public class CentroCostoController : BaseController // Heredamos de BaseController
    {
        private readonly ApplicationDbContext _context;

        public CentroCostoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Usamos la propiedad EsAdminGlobal del BaseController
            ViewBag.EsAdmin = EsAdminGlobal;
            return View();
        }

        #region MÉTODOS DE CONSULTA (TREEVIEW)

        [HttpGet]
        public JsonResult GetArbolCentrosCosto()
        {
            try
            {
                var nodos = new List<object>();

                // 1. OBTENER EMPRESAS (Nodos Raíz)
                // Lógica manual: Si no es Admin Global, filtramos por el ID de empresa del usuario
                var qEmpresas = _context.Empresas.Where(x => x.Estado == true);

                if (!EsAdminGlobal)
                {
                    qEmpresas = qEmpresas.Where(x => x.Id == EmpresaUsuarioId);
                }

                var listaEmpresas = qEmpresas.ToList();

                // 2. OBTENER CENTROS DE COSTO
                // Misma lógica de filtrado manual
                var qCC = _context.CentroCostos.Where(x => x.Estado == true);

                if (!EsAdminGlobal)
                {
                    qCC = qCC.Where(x => x.EmpresaId == EmpresaUsuarioId);
                }

                var listaCC = qCC.ToList();

                // 3. CONSTRUIR ÁRBOL PARA JSTREE

                // A) Nodos de Empresa
                foreach (var emp in listaEmpresas)
                {
                    nodos.Add(new
                    {
                        id = "EMP_" + emp.Id,
                        parent = "#",
                        text = emp.RazonSocial,
                        icon = "fa fa-building",
                        state = new { opened = true }
                    });
                }

                // B) Nodos de Centros de Costo
                foreach (var cc in listaCC)
                {
                    string parentId = (cc.PadreId == null) ? "EMP_" + cc.EmpresaId : cc.PadreId.ToString();

                    string icono = (cc.EsImputable == true)
                        ? "fa fa-file text-info"
                        : "fa fa-folder text-warning";

                    nodos.Add(new
                    {
                        id = cc.Id.ToString(),
                        parent = parentId,
                        text = $"[{cc.Codigo}] {cc.Nombre}",
                        icon = icono,
                        state = new { opened = false }
                    });
                }

                return Json(nodos);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        #endregion

        #region MÉTODOS CRUD (SOLO PARA ADMIN GLOBAL)

        [HttpGet]
        public JsonResult ListarTodo()
        {
            // CORRECCIÓN: USAR LINQ JOINS EN LUGAR DE INCLUDE
            // Cruzamos manualmente las tablas por sus IDs ya que no hay FKs físicas.

            var query = from cc in _context.CentroCostos
                        join emp in _context.Empresas on cc.EmpresaId equals emp.Id
                        // Left Join con TipoCuenta para obtener el nombre de la cuenta (si existe)
                        join tc in _context.TipoCuentas on cc.TipoCuentaId equals tc.Id into tcJoin
                        from tc in tcJoin.DefaultIfEmpty()
                            // Left Join con CentroCosto (a sí mismo) para obtener el nombre del Padre
                        join padre in _context.CentroCostos on cc.PadreId equals padre.Id into padreJoin
                        from padre in padreJoin.DefaultIfEmpty()
                        where cc.Estado == true
                        select new { cc, emp, tc, padre };

            // Filtro de seguridad usando las variables del BaseController
            if (!EsAdminGlobal)
            {
                query = query.Where(x => x.cc.EmpresaId == EmpresaUsuarioId);
            }

            // Proyección final
            var data = query.Select(x => new
            {
                x.cc.Id,
                x.cc.Codigo,
                x.cc.Nombre,
                // Si tiene padre, mostramos nombre del padre. Si es raíz, mostramos nombre de la Empresa.
                Padre = x.padre != null ? x.padre.Nombre : x.emp.RazonSocial,
                x.tc.NumeroCuenta,
                // Nombre del Tipo de Cuenta (si existe)
                TipoCuenta = x.tc != null ? x.tc.Nombre : "-",
                x.cc.EsImputable,
                x.cc.EmpresaId,
                Empresa = x.emp.RazonSocial,
                CuentaAbono = x.cc.CuentaAbono ?? "-",
                CuentaCargo = x.cc.CuentaCargo ?? "-",
                FechaInicio = x.cc.FechaInicio.HasValue ? x.cc.FechaInicio.Value.ToString("dd/MM/yyyy") : "-",
                FechaFin = x.cc.FechaFin.HasValue ? x.cc.FechaFin.Value.ToString("dd/MM/yyyy") : "-",
            })
            .OrderBy(x => x.Codigo)
            .ToList();

            return Json(new { data = data });
        }

        [HttpGet]
        public JsonResult Obtener(int id)
        {
            var obj = _context.CentroCostos.FirstOrDefault(x => x.Id == id);
            return Json(new { status = true, data = obj });
        }

        [HttpPost]
        public JsonResult Guardar(CentroCosto obj)
        {
            // VALIDACIÓN: Solo Admin Global puede editar la estructura maestra
            if (!EsAdminGlobal)
            {
                return Json(new { status = false, message = "ACCESO DENEGADO: Solo el Administrador Global puede gestionar Centros de Costo." });
            }

            try
            {
                if (obj.Id == 0)
                {
                    // Validación manual de duplicidad sin FKs
                    bool existe = _context.CentroCostos.Any(x => x.Codigo == obj.Codigo && x.EmpresaId == obj.EmpresaId && x.Estado == true);
                    if (existe) return Json(new { status = false, message = "El código ya existe en la empresa seleccionada." });

                    obj.Estado = true;
                    obj.FechaRegistro = DateTime.Now;
                    _context.CentroCostos.Add(obj);
                }
                else
                {
                    var temp = _context.CentroCostos.FirstOrDefault(x => x.Id == obj.Id);
                    if (temp == null) return Json(new { status = false, message = "Registro no encontrado." });

                    temp.Codigo = obj.Codigo;
                    temp.Nombre = obj.Nombre;
                    temp.PadreId = obj.PadreId;
                    temp.EmpresaId = obj.EmpresaId;
                    temp.EsImputable = obj.EsImputable;

                    // Campos nuevos
                    temp.FechaInicio = obj.FechaInicio;
                    temp.FechaFin = obj.FechaFin;
                    temp.TipoCuentaId = obj.TipoCuentaId;
                    temp.CuentaCargo = obj.CuentaCargo;
                    temp.CuentaAbono = obj.CuentaAbono;

                    _context.CentroCostos.Update(temp);
                }

                _context.SaveChanges();
                return Json(new { status = true, message = "Operación realizada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            if (!EsAdminGlobal)
            {
                return Json(new { status = false, message = "ACCESO DENEGADO." });
            }

            // Validar hijos activos manualmente
            bool tieneHijos = _context.CentroCostos.Any(x => x.PadreId == id && x.Estado == true);
            if (tieneHijos) return Json(new { status = false, message = "No se puede eliminar: El Centro de Costo tiene sub-niveles dependientes." });

            var obj = _context.CentroCostos.FirstOrDefault(x => x.Id == id);
            if (obj != null)
            {
                obj.Estado = false;
                _context.SaveChanges();
                return Json(new { status = true, message = "Eliminado correctamente." });
            }
            return Json(new { status = false, message = "No se pudo eliminar el registro." });
        }

        #endregion

        #region COMBOS (DROPDOWNS)

        [HttpGet]
        public JsonResult GetComboPadres(int? empresaId)
        {
            var q = _context.CentroCostos.Where(x => x.Estado == true);

            if (empresaId != null)
            {
                q = q.Where(x => x.EmpresaId == empresaId);
            }

            var lista = q.Select(x => new
            {
                id = x.Id,
                text = x.Codigo + " - " + x.Nombre
            }).OrderBy(x => x.text).ToList();

            return Json(lista);
        }

        [HttpGet]
        public JsonResult GetComboTipoCuenta()
        {
            var lista = _context.TipoCuentas
                .Where(x => x.Estado == true)
                .Select(x => new { id = x.Id, text = x.Codigo + " - " + x.Nombre + " - " + x.NumeroCuenta })
                .ToList();
            return Json(lista);
        }

        [HttpGet]
        public JsonResult GetComboEmpresas()
        {
            // Solo para el Admin Global
            var q = _context.Empresas.Where(x => x.Estado == true);

            if (!EsAdminGlobal)
            {
                // Si no es admin, filtramos solo su empresa
                q = q.Where(x => x.Id == EmpresaUsuarioId);
            }

            var lista = q.Select(x => new { id = x.Id, text = x.RazonSocial }).ToList();
            return Json(lista);
        }

        #endregion
    }
}