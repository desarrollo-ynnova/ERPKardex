using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_documento_interno")]
    public class TipoDocumentoInterno
    {
        [Key]
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        [Column("ultimo_correlativo")]
        public int? UltimoCorrelativo { get; set; }
        [Column("tipo_documento_id")]
        public int? TipoDocumentoId { get; set; }
        public bool? Estado { get; set; }
    }
}
