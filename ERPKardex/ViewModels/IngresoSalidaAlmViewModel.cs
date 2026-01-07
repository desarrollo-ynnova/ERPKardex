namespace ERPKardex.ViewModels
{
    public class IngresoSalidaAlmViewModel
    {
        public int Id { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Numero { get; set; }
        public int? MotivoId { get; set; }
        public string? CodMotivo { get; set; }
        public bool? TipoMovimiento { get; set; }
        public string? Motivo { get; set; }
        public int? TipoDocumentoId { get; set; }
        public DateTime? FechaDocumento { get; set; }
        public string? TipoDocumento { get; set; }
        public string? SerieDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public int? TipoDocumentoValorizacionId { get; set; }
        public DateTime? FechaDocumentoValorizacion { get; set; }
        public string? TipoDocumentoValorizacion { get; set; }
        public string? SerieDocumentoValorizacion { get; set; }
        public string? NumeroDocumentoValorizacion { get; set; }
        public int? MonedaId { get; set; }
        public string? Moneda { get; set; }
        public int? EstadoId { get; set; }
        public string? Estado { get; set; }
        public int? SucursalId { get; set; }
        public string? Sucursal { get; set; }
        public int? AlmacenId { get; set; }
        public string? Almacen { get; set; }
        public int? UsuarioId { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}
