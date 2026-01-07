using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Stock_almacen")]
    public class StockAlmacen
    {
        [Key]
        public int Id { get; set; }
        [Column("almacen_id")]
        public int AlmacenId { get; set; }
        [Column("cod_producto")]
        public string? CodProducto { get; set; }
        [Column("stock_actual")]
        public decimal? StockActual { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("ultima_actualizacion")]
        public DateTime? UltimaActualizacion { get; set; }
    }
}
