using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("detraccion")]
    public class Detraccion
    {
        [Key] public int Id { get; set; }
        [Column("tipo_id")] public int? TipoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Porcentaje { get; set; }
        [Column("importe_minimo")] public decimal? ImporteMinimo { get; set; }
        [Column("porcentaje_uit")] public decimal? PorcentajeUit { get; set; }
        public bool? Estado { get; set; }
    }
}
