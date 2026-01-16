using ERPKardex.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ERPKardex.Services
{
    public interface IPermisoService
    {
        Task<bool> TienePermiso(string codigoPermiso);
        void LimpiarCacheUsuario(int empresaUsuarioId);
    }

    public class PermisoService : IPermisoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IMemoryCache _cache;

        public PermisoService(ApplicationDbContext context, IHttpContextAccessor httpContext, IMemoryCache cache)
        {
            _context = context;
            _httpContext = httpContext;
            _cache = cache;
        }

        public async Task<bool> TienePermiso(string codigoPermiso)
        {
            var user = _httpContext.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated) return false;

            // 1. BYPASS ADMINISTRADOR (Lectura desde Claim, CERO BD)
            var adminClaim = user.FindFirst("EsAdministrador");
            if (adminClaim != null && adminClaim.Value == "true") return true;

            // 2. OBTENER ID VÍNCULO
            var claimVinculo = user.FindFirst("EmpresaUsuarioId");
            if (claimVinculo == null) return false;

            int idVinculo = int.Parse(claimVinculo.Value);

            // 3. CACHÉ (Memoria RAM)
            string cacheKey = $"PERMISOS_EU_{idVinculo}";

            if (!_cache.TryGetValue(cacheKey, out List<string> misPermisos))
            {
                // 4. SI NO ESTÁ EN RAM, CONSULTAR BD (JOIN EXPLÍCITO / SIN VIRTUAL)
                misPermisos = await (from eup in _context.EmpresaUsuarioPermisos
                                     join p in _context.Permisos on eup.PermisoId equals p.Id
                                     where eup.EmpresaUsuarioId == idVinculo
                                        && p.Estado == true // Aseguramos que el permiso esté activo
                                     select p.Codigo).ToListAsync();

                // Guardar en RAM por 20 min
                var ops = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(20));
                _cache.Set(cacheKey, misPermisos, ops);
            }

            return misPermisos.Contains(codigoPermiso);
        }

        public void LimpiarCacheUsuario(int empresaUsuarioId)
        {
            _cache.Remove($"PERMISOS_EU_{empresaUsuarioId}");
        }
    }
}