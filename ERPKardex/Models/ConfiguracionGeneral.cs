using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("configuracion_general")]
    public class ConfiguracionGeneral
    {
        [Key] public int Id { get; set; }
        public string Clave { get; set; }
        public string Valor { get; set; }
        public string? Descripcion { get; set; }
        [Column("fecha_registro")] public DateTime? FechaRegistro { get; set; }
    }
}
