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

        // CORRELATIVO VISUAL (001, 002...)
        [Column("item")]
        public string? Item { get; set; }

        // TRAZABILIDAD (CRÍTICO PARA ATENCIÓN PARCIAL)
        // Aquí guardamos el ID exacto de la línea de la orden que estamos facturando
        [Column("id_referencia")]
        public int? IdReferencia { get; set; } // ID de dordencompra/dordenservicio

        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; } // "DORDENCOMPRA"

        // DATOS DEL ÍTEM (Copiados de la orden)
        [Column("producto_id")]
        public int? ProductoId { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }

        // CANTIDADES Y PRECIOS
        // Cantidad: Lo que se está facturando AHORA (ej: 250)
        [Column("cantidad")]
        public decimal? Cantidad { get; set; }

        [Column("precio_unitario")]
        public decimal? PrecioUnitario { get; set; }

        [Column("total")]
        public decimal? Total { get; set; }
    }
}