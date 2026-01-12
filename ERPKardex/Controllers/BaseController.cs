using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERPKardex.Controllers
{
    public class BaseController : Controller
    {
        // Propiedad para obtener el ID de empresa del usuario logueado
        protected int EmpresaUsuarioId
        {
            get
            {
                var claim = User.FindFirst("EmpresaId");
                return claim != null ? int.Parse(claim.Value) : 0;
            }
        }

        // Propiedad para obtener el ID del usuario logueado
        protected int UsuarioActualId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                return claim != null ? int.Parse(claim.Value) : 0;
            }
        }

        // Aquí definimos la lógica del "Admin Global" (Hardcodeado solo una vez aquí)
        protected bool EsAdminGlobal => EmpresaUsuarioId == 4;
    }
}