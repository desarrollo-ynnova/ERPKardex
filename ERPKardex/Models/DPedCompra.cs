using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dpedcompra")]
    public class DPedCompra
    {
        [Key]
        public int Id { get; set; }
        [Column("pedcompra_id")]
        public int? PedidoCompraId { get; set; }
        public string? Item { get; set; }
        [Column("producto_id")]
        public int? ProductoId { get; set; }
        [Column("descripcion_libre")]
        public string? DescripcionLibre { get; set; }
        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }
        [Column("cantidad_solicitada")]
        public decimal? CantidadSolicitada { get; set; }
        [Column("cantidad_aprobada")]
        public decimal? CantidadAprobada { get; set; }
        [Column("observacion_item")]
        public string? ObservacionItem { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}
