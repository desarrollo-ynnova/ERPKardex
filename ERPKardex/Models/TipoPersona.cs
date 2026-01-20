using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_persona")]
    public class TipoPersona
    {
        [Key]
        public int Id { get; set; }

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; } = DateTime.Now;
    }
}