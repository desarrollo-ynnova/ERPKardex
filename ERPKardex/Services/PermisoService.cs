using ERPKardex.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ERPKardex.Services
{
    public interface IPermisoService
    {
        Task<bool> TienePermiso(string codigoPermiso);
        void LimpiarCacheUsuario(int empresaUsuarioId); // Para cuando editas permisos en el panel
    }

    public class PermisoService : IPermisoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IMemoryCache _cache; // <--- LA MAGIA

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

            // 1. Obtener ID del Vínculo desde la Cookie (Súper rápido)
            var claimVinculo = user.FindFirst("EmpresaUsuarioId");
            if (claimVinculo == null) return false;

            int idVinculo = int.Parse(claimVinculo.Value);

            // 2. BUSCAR EN CACHÉ (Memoria RAM)
            // La clave será única por usuario en esa empresa: "PERMISOS_15"
            string cacheKey = $"PERMISOS_{idVinculo}";

            if (!_cache.TryGetValue(cacheKey, out List<string> permisosDelUsuario))
            {
                // 3. SI NO ESTÁ EN CACHÉ, VAMOS A LA BD (Solo pasa 1 vez cada 30 min)
                permisosDelUsuario = await (from eup in _context.EmpresaUsuarioPermisos
                                            join p in _context.Permisos on eup.PermisoId equals p.Id
                                            where eup.EmpresaUsuarioId == idVinculo
                                            select p.Codigo).ToListAsync();

                // Guardamos en RAM por 30 minutos (Configurable)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(cacheKey, permisosDelUsuario, cacheOptions);
            }

            // 4. VALIDACIÓN EN MEMORIA (Extremadamente rápida)
            return permisosDelUsuario.Contains(codigoPermiso);
        }

        public void LimpiarCacheUsuario(int empresaUsuarioId)
        {
            // Este método lo llamarás cuando un Admin le cambie los permisos a alguien
            // para obligar al sistema a recargar desde la BD en la siguiente petición.
            _cache.Remove($"PERMISOS_{empresaUsuarioId}");
        }
    }
}