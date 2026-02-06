using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_orden_pago")]
    public class TipoOrdenPago
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public bool? Estado { get; set; }
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}
