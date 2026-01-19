using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("documento_pagar")]
    public class DocumentoPagar
    {
        [Key]
        public int Id { get; set; }

        // DATOS DE FILTRO
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("proveedor_id")]
        public int ProveedorId { get; set; }

        [Column("tipo_documento_interno_id")]
        public int TipoDocumentoInternoId { get; set; } // FAC, ANT, NC, LET

        // VÍNCULOS LÓGICOS (IDs directos)
        [Column("orden_compra_id")]
        public int? OrdenCompraId { get; set; }

        [Column("orden_servicio_id")]
        public int? OrdenServicioId { get; set; }

        [Column("documento_referencia_id")]
        public int? DocumentoReferenciaId { get; set; } // Para Notas de Crédito (Referencia a la Factura)

        // DATOS FÍSICOS
        [Column("serie")]
        public string? Serie { get; set; }

        [Column("numero")]
        public string? Numero { get; set; }

        [Column("fecha_emision")]
        public DateTime? FechaEmision { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("moneda_id")]
        public int? MonedaId { get; set; }

        [Column("tipo_cambio")]
        public decimal? TipoCambio { get; set; }

        // IMPORTES
        [Column("total")]
        public decimal Total { get; set; } = 0; // Monto físico del papel

        [Column("saldo")]
        public decimal Saldo { get; set; } = 0; // Monto pendiente de pagar o aplicar

        // AUDITORÍA
        [Column("estado_id")]
        public int? EstadoId { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("usuario_registro_id")]
        public int? UsuarioRegistroId { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}