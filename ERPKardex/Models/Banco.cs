using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("banco")]
    public class Banco
    {
        [Key]
        public int Id { get; set; }

        public string? Ruc { get; set; } // Ej: "002"

        [Column("nombre")]
        public string? Nombre { get; set; } // Ej: "BCP"

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}