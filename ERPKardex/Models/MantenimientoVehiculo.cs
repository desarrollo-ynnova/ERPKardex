using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("mantenimiento_vehiculo")]
    public class MantenimientoVehiculo
    {
        [Key]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("fecha", TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Column("tipo_mantenimiento")]
        [StringLength(30)]
        public string TipoMantenimiento { get; set; }

        [Column("km_mantenimiento", TypeName = "decimal(12,2)")]
        public decimal? KmMantenimiento { get; set; }

        [Column("km_al_servicio", TypeName = "decimal(12,2)")]
        public decimal? KmAlServicio { get; set; }

        [Column("trabajos_ejecutados")]
        [StringLength(1000)]
        public string? TrabajosEjecutados { get; set; }

        [Column("precio", TypeName = "decimal(12,2)")]
        public decimal? Precio { get; set; }

        [Column("moneda")]
        [StringLength(5)]
        public string? Moneda { get; set; }

        [Column("conductor")]
        [StringLength(200)]
        public string? Conductor { get; set; }

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