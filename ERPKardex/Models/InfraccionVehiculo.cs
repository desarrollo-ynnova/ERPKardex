using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("infraccion_vehiculo")]
    public class InfraccionVehiculo
    {
        [Key]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("entidad")]
        [StringLength(30)]
        public string Entidad { get; set; }

        [Column("nro_papeleta")]
        [StringLength(50)]
        public string? NroPapeleta { get; set; }

        [Column("fecha_ocurrencia", TypeName = "date")]
        public DateTime? FechaOcurrencia { get; set; }

        [Column("codigo_infraccion")]
        [StringLength(20)]
        public string? CodigoInfraccion { get; set; }

        [Column("descripcion_falta")]
        [StringLength(500)]
        public string? DescripcionFalta { get; set; }

        [Column("conductor_datos")]
        [StringLength(200)]
        public string? ConductorDatos { get; set; }

        [Column("ruc_dni_infractor")]
        [StringLength(20)]
        public string? RucDniInfractor { get; set; }

        [Column("importe", TypeName = "decimal(12,2)")]
        public decimal? Importe { get; set; }

        [Column("situacion_pago")]
        [StringLength(30)]
        public string? SituacionPago { get; set; }

        [Column("fecha_reporte", TypeName = "date")]
        public DateTime? FechaReporte { get; set; }

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