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

        [Column("movimiento_activo_id")]
        public int MovimientoActivoId { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("condicion_item")]
        public string? CondicionItem { get; set; }

        [Column("medida_registro")]
        public decimal MedidaRegistro { get; set; }

        [Column("observacion_item")]
        public string? ObservacionItem { get; set; }
    }
}