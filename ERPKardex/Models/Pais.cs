using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("pais")]
    public class Pais
    {
        [Key]
        public int Id { get; set; }

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}