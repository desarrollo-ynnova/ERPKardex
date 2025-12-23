using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Motivo")]
    public class Motivo
    {
        [Key]
        public int Id { get; set; }
        public string? Codigo { get; set; }
        [Column("tipo_movimiento")]
        public bool? TipoMovimiento { get; set; }
        public string? Descripcion { get; set; }
        public bool? Estado { get; set; }
    }
}
