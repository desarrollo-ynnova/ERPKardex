using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo_especificacion")]
    public class ActivoEspecificacion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int? ActivoId { get; set; }

        [Column("clave")]
        public string? Clave { get; set; }  // Ej: "PROCESADOR"

        [Column("valor")]
        public string? Valor { get; set; }  // Ej: "INTEL CORE I7"
    }
}