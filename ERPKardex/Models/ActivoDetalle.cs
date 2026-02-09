using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo_detalle")]
    public class ActivoDetalle
    {
        [Key]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("clave")]
        [StringLength(100)]
        public string Clave { get; set; }

        [Column("valor")]
        [StringLength(1000)]
        public string? Valor { get; set; }

        [Column("orden")]
        public int? Orden { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }
}