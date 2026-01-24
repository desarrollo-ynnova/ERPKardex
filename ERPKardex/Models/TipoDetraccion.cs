using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_detraccion")]
    public class TipoDetraccion
    {
        [Key] public int Id { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
    }
}
