using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo_asignacion")]
    public class ActivoAsignacion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int? ActivoId { get; set; }

        // Asignación: Persona O Área O Stock
        [Column("personal_id")]
        public int? PersonalId { get; set; }

        [Column("centro_costo_id")]
        public int? CentroCostoId { get; set; }

        [Column("es_stock")]
        public bool? EsStock { get; set; }

        // Fechas
        [Column("fecha_asignacion")]
        public DateTime? FechaAsignacion { get; set; }

        [Column("fecha_devolucion")]
        public DateTime? FechaDevolucion { get; set; } // NULL = Vigente

        // Ubicación Física y Observaciones
        [Column("ubicacion_texto")]
        public string? UbicacionTexto { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        // Control
        [Column("estado")]
        public bool? Estado { get; set; }

        [Column("usuario_registro_id")]
        public int? UsuarioRegistroId { get; set; }

        [Column("ruta_acta")]
        public string? RutaActa { get; set; }
    }
}