using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo_historial_medida")]
    public class ActivoHistorialMedida
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("fecha_lectura")]
        public DateTime? FechaLectura { get; set; }

        [Column("valor_medida")]
        public decimal ValorMedida { get; set; }

        [Column("origen_dato")]
        public string? OrigenDato { get; set; } // ENTREGA, CONTROL_SEMANAL

        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("usuario_registro_id")]
        public int? UsuarioRegistroId { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}