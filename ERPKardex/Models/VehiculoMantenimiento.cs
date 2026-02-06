using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("vehiculo_mantenimiento")]
    public class VehiculoMantenimiento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("fecha_servicio")]
        public DateTime FechaServicio { get; set; }

        [Column("tipo")]
        public string? Tipo { get; set; } // Preventivo / Correctivo

        // Tu lógica crítica de control
        [Column("km_programado")]
        public decimal? KmProgramado { get; set; }

        [Column("km_real_servicio")]
        public decimal? KmRealServicio { get; set; }

        [Column("trabajos_realizados")]
        public string? TrabajosRealizados { get; set; }

        [Column("costo_total")]
        public decimal CostoTotal { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        // Snapshot (Foto del momento)
        [Column("conductor_asignado")]
        public string? ConductorAsignado { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}