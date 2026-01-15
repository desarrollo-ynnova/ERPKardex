using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("moneda")]
    public class Moneda
    {
        [Key]
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Simbolo { get; set; }
        public bool? Estado { get; set; }
    }
}