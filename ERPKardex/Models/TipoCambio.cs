using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_cambio")]
    public class TipoCambio
    {
        [Key]
        public int Id { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("tc_compra", TypeName = "decimal(12,6)")]
        public decimal TcCompra { get; set; }

        [Column("tc_venta", TypeName = "decimal(12,6)")]
        public decimal TcVenta { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}