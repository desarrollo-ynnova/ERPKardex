using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dasiento_contable")]
    public class DasientoContable
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("asiento_contable_id")]
        public int AsientoContableId { get; set; }

        [Column("cuenta_contable_id")]
        public int CuentaContableId { get; set; }

        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }

        [Column("cliente_id")]
        public int? ClienteId { get; set; }

        [Column("proveedor_id")]
        public int? ProveedorId { get; set; }

        [Column("tipo_documento_id")]
        public int? TipoDocumentoId { get; set; }

        [Column("serie_documento")]
        public string? SerieDocumento { get; set; }

        [Column("numero_documento")]
        public string? NumeroDocumento { get; set; }

        [Column("fecha_emision")]
        public DateTime? FechaEmision { get; set; }

        [Column("debe_soles")]
        public decimal DebeSoles { get; set; }

        [Column("haber_soles")]
        public decimal HaberSoles { get; set; }

        [Column("debe_dolares")]
        public decimal DebeDolares { get; set; }

        [Column("haber_dolares")]
        public decimal HaberDolares { get; set; }

        [Column("glosa_detalle")]
        public string? GlosaDetalle { get; set; }

        // Navegación (ForeignKey)
        [ForeignKey("AsientoContableId")]
        public virtual AsientoContable? AsientoContable { get; set; }
    }
}