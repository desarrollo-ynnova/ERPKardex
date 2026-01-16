using ERPKardex.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERPKardex.Filters
{
    // Atributo: Lo que pones encima del Controller [AuthorizePermiso("CODIGO")]
    public class AuthorizePermisoAttribute : TypeFilterAttribute
    {
        public AuthorizePermisoAttribute(string codigoPermiso) : base(typeof(AuthorizePermisoFilter))
        {
            Arguments = new object[] { codigoPermiso };
        }
    }

    // Filtro: La lógica que ejecuta .NET
    public class AuthorizePermisoFilter : IAsyncAuthorizationFilter
    {
        private readonly string _codigoPermiso;
        private readonly IPermisoService _permisoService;

        public AuthorizePermisoFilter(string codigoPermiso, IPermisoService permisoService)
        {
            _codigoPermiso = codigoPermiso;
            _permisoService = permisoService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            bool acceso = await _permisoService.TienePermiso(_codigoPermiso);

            if (!acceso)
            {
                if (IsAjax(context.HttpContext.Request))
                    context.Result = new JsonResult(new { status = false, message = "⛔ Acceso Denegado: Permiso insuficiente." });
                else
                    context.Result = new StatusCodeResult(403); // Forbidden
            }
        }

        private bool IsAjax(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}