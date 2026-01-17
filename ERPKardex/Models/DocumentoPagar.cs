using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("documento_pagar")]
    public class DocumentoPagar
    {
        public int Id { get; set; }
        public int TipoDocumentoInternoId { get; set; }
        public string? CodigoInterno { get; set; }
        public int EmpresaId { get; set; }
        public int ProveedorId { get; set; }
        public int TipoDocumentoId { get; set; }
        public string? Serie { get; set; }
        public string? Numero { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaContable { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int MonedaId { get; set; }
        public decimal TipoCambio { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal TotalInafecto { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoPendiente { get; set; }

        public int? OrdenCompraId { get; set; }
        public int? OrdenServicioId { get; set; }
        public int? DocReferenciaId { get; set; }

        public int EstadoId { get; set; }
        public int EstadoPagoId { get; set; }
        public string? Glosa { get; set; }
        public int UsuarioCreacionId { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}