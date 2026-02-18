namespace ERPKardex.Models.ViewModels
{
    public class EstadoResultadosViewModel
    {
        // A. Ingresos
        public decimal VentasNetas { get; set; } // Cuenta 70
        public decimal OtrosIngresos { get; set; } // Cuentas 75, 76, 77
        public decimal TotalIngresos => VentasNetas + OtrosIngresos;

        // B. Costos
        public decimal CostoVentas { get; set; } // Cuenta 69
        public decimal UtilidadBruta => TotalIngresos - CostoVentas;

        // C. Gastos Generales y Administrativos
        public decimal GastosVentas { get; set; } // Cuenta 95
        public decimal GastosAdministrativos { get; set; } // Cuenta 94
        public decimal GastosFinancieros { get; set; } // Cuenta 97
        public decimal TotalGastos => GastosVentas + GastosAdministrativos + GastosFinancieros;

        // D. Otros Ingresos/Gastos Netos
        public decimal OtrosGastosNetos { get; set; } // Cuenta 65 (Gastos Diversos, tu famosa cerveza)

        // UTILIDAD FINAL
        public decimal UtilidadNeta => UtilidadBruta - TotalGastos - OtrosGastosNetos;
    }
}