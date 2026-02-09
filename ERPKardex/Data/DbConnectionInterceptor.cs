using ERPKardex.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace ERPKardex.Data // <--- Tu namespace
{
    public class AuditoriaInterceptor : DbConnectionInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Se ejecuta al abrir conexión SÍNCRONA
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            InjectarContexto(connection);
            base.ConnectionOpened(connection, eventData);
        }

        // Se ejecuta al abrir conexión ASÍNCRONA (Lo más común en web)
        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            InjectarContexto(connection);
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        private void InjectarContexto(DbConnection connection)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return; // Si es una tarea en background, salimos

            try
            {
                // A. Capturamos Datos
                string ip = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

                // Si usas un Proxy inverso (raro en IIS local puro, pero por si acaso)
                if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                    ip = context.Request.Headers["X-Forwarded-For"];

                string ua = context.Request.Headers["User-Agent"].ToString();
                if (ua.Length > 250) ua = ua.Substring(0, 250); // Evitar error por longitud

                // B. Capturamos la MAC usando nuestro Helper
                string mac = NetworkHelper.GetMacAddress(ip);

                // C. INYECCIÓN SQL (Sin EF Core, directo al driver)
                // Usamos sp_set_session_context para guardar los datos en la RAM de la conexión actual
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        EXEC sp_set_session_context 'IP_Cliente', @ip;
                        EXEC sp_set_session_context 'UserAgent', @ua;
                        EXEC sp_set_session_context 'MAC_Cliente', @mac;";

                    var pIp = cmd.CreateParameter(); pIp.ParameterName = "@ip"; pIp.Value = ip; cmd.Parameters.Add(pIp);
                    var pUa = cmd.CreateParameter(); pUa.ParameterName = "@ua"; pUa.Value = ua; cmd.Parameters.Add(pUa);
                    var pMac = cmd.CreateParameter(); pMac.ParameterName = "@mac"; pMac.Value = mac; cmd.Parameters.Add(pMac);

                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Silenciamos errores de auditoría para no tumbar la app si falla el ARP
            }
        }
    }
}