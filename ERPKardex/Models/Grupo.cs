using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Grupo")]
    public class Grupo
    {
        [Key]
        public int Id { get; set; }
        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("tipo_existencia_id")]
        public int? TipoExistenciaId { get; set; }
        [Column("cuenta_id")]
        public int? CuentaId { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}