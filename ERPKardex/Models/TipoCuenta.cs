using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_cuenta")]
    public class TipoCuenta
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        [Column("numero_cuenta")]
        public string? NumeroCuenta { get; set; }
        public bool? Estado { get; set; }
    }
}
