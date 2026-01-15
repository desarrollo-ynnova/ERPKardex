using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("empresa_usuario_permiso")]
    public class EmpresaUsuarioPermiso
    {
        [Key]
        public int Id { get; set; }

        // FK hacia la tabla intermedia 'empresa_usuario'
        [Column("empresa_usuario_id")]
        public int EmpresaUsuarioId { get; set; }

        // FK hacia la tabla 'permiso'
        [Column("permiso_id")]
        public int PermisoId { get; set; }
    }
}