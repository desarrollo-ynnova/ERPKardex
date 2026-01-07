using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("dpedservicio")]
    public class DPedServicio
    {
        [Key]
        public int Id { get; set; }
        [Column("pedservicio_id")]
        public int? PedidoServicioId { get; set; }
        [Column("producto_id")]
        public int? ProductoId { get; set; }
        public string? Item { get; set; }
        [Column("descripcion_servicio")]
        public string? DescripcionServicio { get; set; }
        public decimal? Cantidad { get; set; }
        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }
        [Column("observacion_item")]
        public string? ObservacionItem { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}
