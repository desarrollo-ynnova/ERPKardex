using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("empresa_usuario")]
    public class EmpresaUsuario
    {
        public int Id { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("usuario_id")]
        public int? UsuarioId { get; set; }
        [Column("tipo_usuario_id")]
        public int? TipoUsuarioId { get; set; }
        public bool? Estado { get; set; }
    }
}
