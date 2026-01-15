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
        public string Codigo { get; set; } // Ej: "MOD_LOGISTICA"

        [Column("descripcion")]
        [StringLength(100)]
        public string Descripcion { get; set; } // Ej: "Acceso al módulo Logística"

        [Column("modulo")]
        [StringLength(50)]
        public string Modulo { get; set; } // Ej: "LOGISTICA", "FINANZAS"

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}