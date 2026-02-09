using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("bitacora_kilometraje")]
    public class BitacoraKilometraje
    {
        [Key]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("kilometraje", TypeName = "decimal(12,2)")]
        public decimal? Kilometraje { get; set; }

        [Column("observacion")]
        [StringLength(500)]
        public string? Observacion { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }
    }
}