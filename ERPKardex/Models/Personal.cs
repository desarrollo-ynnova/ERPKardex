using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("personal")]
    public class Personal
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("dni")]
        public string? Dni { get; set; }

        [Column("nombres_completos")]
        public string? NombresCompletos { get; set; }

        [Column("cargo")]
        public string? Cargo { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}