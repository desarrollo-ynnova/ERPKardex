using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo_tipo")]
    public class ActivoTipo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string? Nombre { get; set; }

        [Column("grupo_id")]
        public int? GrupoId { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}