using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Tipo_documento")]
    public class TipoDocumento
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public bool? Estado { get; set; }
    }
}
