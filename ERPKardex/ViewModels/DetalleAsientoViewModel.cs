namespace ERPKardex.ViewModels
{
    public class DetalleAsientoViewModel
    {
        public int CuentaContableId { get; set; }
        public int? CentroCostoId { get; set; }
        public int? ClienteId { get; set; }
        public int? ProveedorId { get; set; }
        public int? TipoDocumentoId { get; set; }
        public string? SerieDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public DateTime? FechaEmision { get; set; }
        public decimal DebeSoles { get; set; }
        public decimal HaberSoles { get; set; }
        public decimal DebeDolares { get; set; }
        public decimal HaberDolares { get; set; }
        public string? GlosaDetalle { get; set; }
    }
}
