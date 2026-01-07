using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Cuenta")]
    public class Cuenta
    {
        [Key]
        public int Id { get; set; }
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}