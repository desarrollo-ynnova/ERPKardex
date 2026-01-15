using ERPKardex.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERPKardex.Filters
{
    // 1. EL DECORADOR (Lo que escribes encima del Controller)
    public class AuthorizePermisoAttribute : TypeFilterAttribute
    {
        public AuthorizePermisoAttribute(string codigoPermiso) : base(typeof(AuthorizePermisoFilter))
        {
            // Pasamos el código del permiso (ej: "MOD_LOGISTICA") al filtro real
            Arguments = new object[] { codigoPermiso };
        }
    }

    // 2. EL FILTRO REAL (Donde ocurre la magia y la Inyección de Dependencias)
    public class AuthorizePermisoFilter : IAsyncAuthorizationFilter
    {
        private readonly string _codigoPermiso;
        private readonly IPermisoService _permisoService; // ¡Aquí inyectamos tu servicio con Caché!

        public AuthorizePermisoFilter(string codigoPermiso, IPermisoService permisoService)
        {
            _codigoPermiso = codigoPermiso;
            _permisoService = permisoService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // A. Si el usuario no está logueado, adiós.
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult(); // 401
                return;
            }

            // B. Usamos tu servicio optimizado (lee de RAM o BD una sola vez)
            bool tieneAcceso = await _permisoService.TienePermiso(_codigoPermiso);

            if (!tieneAcceso)
            {
                // C. RESPUESTA INTELIGENTE
                // Si la petición viene de AJAX (jQuery), devolvemos JSON para que no rompa el front.
                // Si es navegación normal, devolvemos una vista de error o redirigimos.

                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new { status = false, message = "⛔ Acceso Denegado: No tienes el permiso " + _codigoPermiso });
                }
                else
                {
                    // Opción 1: Redirigir al Home con error
                    // context.Result = new RedirectToActionResult("Index", "Home", new { error = "SinPermiso" });

                    // Opción 2: Retornar código 403 (Forbidden)
                    context.Result = new StatusCodeResult(403);
                }
            }
        }

        // Helper para detectar si es AJAX (jQuery suele enviar este header)
        private bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   request.Headers["Accept"].ToString().Contains("application/json");
        }
    }
}