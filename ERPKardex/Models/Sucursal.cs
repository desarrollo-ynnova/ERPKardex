using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Sucursal")]
    public class Sucursal
    {
        [Key]
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}
