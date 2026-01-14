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

        [Column("tc_compra")]
        public decimal TcCompra { get; set; }

        [Column("tc_venta")]
        public decimal TcVenta { get; set; } // <--- Este es el importante para el sistema

        [Column("estado")]
        public bool? Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}