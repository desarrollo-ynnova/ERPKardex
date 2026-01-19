using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ddocumento_pagar")]
    public class DDocumentoPagar
    {
        [Key]
        public int Id { get; set; }

        [Column("documento_pagar_id")]
        public int DocumentoPagarId { get; set; }

        // ==========================================================
        // CORRECCIÓN SOLICITADA: REFERENCIA DINÁMICA
        // ==========================================================
        [Column("id_referencia")]
        public int? IdReferencia { get; set; } // ID de dordencompra o dordenservicio

        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; } // "DORDENCOMPRA" o "DORDENSERVICIO"

        // DATOS COPIADOS (Snapshot)
        [Column("producto_id")]
        public int? ProductoId { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }

        [Column("cantidad")]
        public decimal? Cantidad { get; set; }

        [Column("precio_unitario")]
        public decimal? PrecioUnitario { get; set; }

        [Column("total")]
        public decimal? Total { get; set; }
    }
}