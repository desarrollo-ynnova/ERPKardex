using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("vehiculo_infraccion")]
    public class VehiculoInfraccion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("entidad")]
        public string? Entidad { get; set; } // SUTRAN, SAT

        [Column("nro_papeleta")]
        public string? NroPapeleta { get; set; }

        [Column("fecha_infraccion")]
        public DateTime? FechaInfraccion { get; set; }

        [Column("codigo_falta")]
        public string? CodigoFalta { get; set; } // M20

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("importe")]
        public decimal Importe { get; set; }

        [Column("situacion_pago")]
        public string? SituacionPago { get; set; } // PENDIENTE, PAGADO

        [Column("conductor_infractor")]
        public string? ConductorInfractor { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}