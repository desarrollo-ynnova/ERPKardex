using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("usuario")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(8)]
        [Column("dni")]
        public string? Dni { get; set; }

        [Required]
        [StringLength(255)]
        [Column("nombre")]
        public string? Nombre { get; set; }

        [StringLength(255)]
        [Column("cargo")]
        public string? Cargo { get; set; } // <--- NUEVO

        [StringLength(255)]
        [Column("email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Column("telefono")]
        public string? Telefono { get; set; } // <--- NUEVO

        [Required]
        [Column("password")]
        public string? Password { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}