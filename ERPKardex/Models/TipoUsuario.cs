using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_usuario")]
    public class TipoUsuario
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        [Column("es_administrador")]
        public bool? EsAdministrador { get; set; }
        public bool? Estado { get; set; }
    }
}
