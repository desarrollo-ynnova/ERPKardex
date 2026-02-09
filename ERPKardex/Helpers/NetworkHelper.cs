using System.Net;
using System.Runtime.InteropServices;

namespace ERPKardex.Helpers // <--- Pon tu namespace real aquí
{
    public static class NetworkHelper
    {
        // Importamos la librería nativa de Windows (iphlpapi.dll) de forma segura
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

        public static string GetMacAddress(string ipAddress)
        {
            try
            {
                // 1. Validaciones para Localhost (Tu propio servidor)
                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
                    return "SERVER-LOCAL";

                // 2. Convertir IP string a entero para la API de Windows
                IPAddress dst = IPAddress.Parse(ipAddress);
                byte[] ipBytes = dst.GetAddressBytes();
                int intAddress = BitConverter.ToInt32(ipBytes, 0);

                // 3. Preparar buffer para recibir la MAC
                byte[] macAddr = new byte[6];
                uint macAddrLen = (uint)macAddr.Length;

                // 4. Disparar el protocolo ARP
                if (SendARP(intAddress, 0, macAddr, ref macAddrLen) != 0)
                    return "NO-ARP-RESPONSE"; // El usuario está offline o hay un firewall bloqueando

                // 5. Formatear bytes a string legible (AA:BB:CC...)
                string[] str = new string[(int)macAddrLen];
                for (int i = 0; i < macAddrLen; i++)
                    str[i] = macAddr[i].ToString("X2");

                return string.Join(":", str);
            }
            catch (Exception)
            {
                return "ERROR-MAC";
            }
        }
    }
}