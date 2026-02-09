using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("movimiento_activo")]
    public class MovimientoActivo
    {
        [Key]
        public int Id { get; set; }

        [Column("codigo")]
        [StringLength(30)]
        public string Codigo { get; set; }

        [Column("tipo_movimiento")]
        [StringLength(20)]
        public string TipoMovimiento { get; set; }

        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("personal_id")]
        public int PersonalId { get; set; }

        [Column("fecha_movimiento")]
        public DateTime FechaMovimiento { get; set; }

        [Column("ruta_acta")]
        [StringLength(500)]
        public string? RutaActa { get; set; }

        [Column("observacion")]
        [StringLength(1000)]
        public string? Observacion { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }
    }
}