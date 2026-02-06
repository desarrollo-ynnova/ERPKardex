using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("vehiculo_seguro")]
    public class VehiculoSeguro
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("aseguradora")]
        public string? Aseguradora { get; set; }

        [Column("nro_poliza")]
        public string? NroPoliza { get; set; }

        [Column("prima_usd")]
        public decimal? PrimaUsd { get; set; }

        [Column("suma_asegurada_usd")]
        public decimal? SumaAseguradaUsd { get; set; }

        [Column("vigencia_fin")]
        public DateTime? VigenciaFin { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}