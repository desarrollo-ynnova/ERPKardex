using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("movimiento_activo")]
    public class MovimientoActivo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo_acta")]
        public string? CodigoActa { get; set; }

        [Column("tipo_movimiento")]
        public string? TipoMovimiento { get; set; } // 'ENTREGA', 'DEVOLUCION'

        [Column("fecha_movimiento")]
        public DateTime? FechaMovimiento { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("personal_id")]
        public int? PersonalId { get; set; } // Receptor o Devolvente

        [Column("usuario_registro_id")]
        public int? UsuarioRegistroId { get; set; }

        [Column("ubicacion_destino")]
        public string? UbicacionDestino { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("ruta_acta_pdf")]
        public string? RutaActaPdf { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}