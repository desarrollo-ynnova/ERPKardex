using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("detalle_ingrediente_activo")]
    public class DetalleIngredienteActivo
    {
        public int Id { get; set; }
        [Column("producto_id")]
        public int? ProductoId { get; set; }

        [Column("ingrediente_activo_id")]
        public int? IngredienteActivoId { get; set; }
        public decimal? Porcentaje { get; set; }
    }
}