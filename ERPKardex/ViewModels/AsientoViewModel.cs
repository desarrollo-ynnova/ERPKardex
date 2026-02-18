namespace ERPKardex.ViewModels
{
    public class AsientoViewModel
    {
        public int EmpresaId { get; set; }
        public int PeriodoId { get; set; }
        public int OrigenAsientoId { get; set; }
        public DateTime FechaContable { get; set; }
        public int MonedaId { get; set; }
        public string Glosa { get; set; }
        public int UsuarioRegistro { get; set; } // Opcional, si lo sacas del token/sesión mejor
        public int? IdReferencia { get; set; }
        public string? TablaReferencia { get; set; }
        public List<DetalleAsientoViewModel> Detalles { get; set; } = new List<DetalleAsientoViewModel>();
    }
}
