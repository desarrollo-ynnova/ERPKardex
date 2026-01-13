using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dmovimiento_activo")]
    public class DMovimientoActivo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("movimiento_id")]
        public int? MovimientoId { get; set; }

        [Column("activo_id")]
        public int? ActivoId { get; set; }

        [Column("condicion_item")]
        public string? CondicionItem { get; set; } // Como estaba al momento de moverlo

        [Column("observacion_item")]
        public string? ObservacionItem { get; set; }
    }
}