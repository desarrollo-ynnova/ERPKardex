using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ciudad")]
    public class Ciudad
    {
        [Key]
        public int Id { get; set; }

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }

        [Column("pais_id")]
        public int? PaisId { get; set; }
    }
}