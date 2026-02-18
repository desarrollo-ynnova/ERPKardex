namespace ERPKardex.Models.ViewModels
{
    public class BalanceGeneralViewModel
    {
        // ACTIVOS (Naturaleza Deudora: Debe - Haber)
        public decimal ActivosCorrientes { get; set; } // Cuentas 10 al 29
        public decimal ActivosNoCorrientes { get; set; } // Cuentas 30 al 39
        public decimal TotalActivos => ActivosCorrientes + ActivosNoCorrientes;

        // PASIVOS (Naturaleza Acreedora: Haber - Debe)
        public decimal PasivosCorrientes { get; set; } // Cuentas 40 al 49
        public decimal PasivosNoCorrientes { get; set; } // Deudas a largo plazo (Opcional en MVP, suele ser subcuentas específicas de la 45)
        public decimal TotalPasivos => PasivosCorrientes + PasivosNoCorrientes;

        // PATRIMONIO (Naturaleza Acreedora: Haber - Debe)
        public decimal PatrimonioNeto { get; set; } // Cuentas 50 al 59

        // EL SECRETO CONTABLE: La Utilidad del Ejercicio
        // El Balance NUNCA va a cuadrar si no le sumas al patrimonio la ganancia/pérdida del Estado de Resultados
        public decimal UtilidadDelEjercicio { get; set; }

        public decimal TotalPatrimonio => PatrimonioNeto + UtilidadDelEjercicio;

        // FÓRMULA FINAL (Total Activos debe ser exactamente igual a esto)
        public decimal TotalPasivoYPatrimonio => TotalPasivos + TotalPatrimonio;
    }
}