using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("seguro_vehiculo")]
    public class SeguroVehiculo
    {
        [Key]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("aseguradora")]
        [StringLength(100)]
        public string? Aseguradora { get; set; }

        [Column("nro_poliza")]
        [StringLength(100)]
        public string? NroPoliza { get; set; }

        [Column("suma_asegurada", TypeName = "decimal(12,2)")]
        public decimal? SumaAsegurada { get; set; }

        [Column("moneda_suma")]
        [StringLength(5)]
        public string? MonedaSuma { get; set; }

        [Column("prima_igv", TypeName = "decimal(12,2)")]
        public decimal? PrimaIgv { get; set; }

        [Column("clase")]
        [StringLength(50)]
        public string? Clase { get; set; }

        [Column("uso")]
        [StringLength(50)]
        public string? Uso { get; set; }

        [Column("fecha_inicio", TypeName = "date")]
        public DateTime? FechaInicio { get; set; }

        [Column("fecha_vigencia", TypeName = "date")]
        public DateTime? FechaVigencia { get; set; }

        [Column("nro_poliza_la_positiva")]
        [StringLength(100)]
        public string? NroPolizaLaPositiva { get; set; }

        [Column("nro_poliza_rimac")]
        [StringLength(100)]
        public string? NroPolizaRimac { get; set; }

        [Column("ajuste_rimac", TypeName = "decimal(12,2)")]
        public decimal? AjusteRimac { get; set; }

        [Column("observacion")]
        [StringLength(500)]
        public string? Observacion { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }
    }
}