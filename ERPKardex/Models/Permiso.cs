using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("permiso")]
    public class Permiso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("codigo")]
        [StringLength(50)]
        public string? Codigo { get; set; }

        [Column("descripcion")]
        [StringLength(100)]
        public string? Descripcion { get; set; }

        [Column("modulo")]
        [StringLength(50)]
        public string? Modulo { get; set; }

        // Aquí mantenemos el ID para saber la jerarquía, 
        // pero la armaremos manualmente en el Controller si hace falta.
        [Column("padre_id")]
        public int? PadreId { get; set; }

        [Column("orden")]
        public int Orden { get; set; } = 0;

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}