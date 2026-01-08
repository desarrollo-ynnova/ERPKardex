using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("pedservicio")]
    public class PedServicio
    {
        [Key]
        public int Id { get; set; }
        [Column("tipo_documento_interno_id")]
        public int? TipoDocumentoInternoId { get; set; }
        public string? Numero { get; set; }
        [Column("fecha_emision")]
        public DateTime? FechaEmision { get; set; }
        [Column("fecha_necesaria")]
        public DateTime? FechaNecesaria { get; set; }
        [Column("usuario_solicitante_id")]
        public int? UsuarioSolicitanteId { get; set; }
        public string? Observacion { get; set; }
        [Column("estado_id")]
        public int? EstadoId { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }
    }
}
