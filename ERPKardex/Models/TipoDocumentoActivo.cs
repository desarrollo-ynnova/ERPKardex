using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_documento_activo")]
    public class TipoDocumentoActivo
    {
        [Key]
        public int Id { get; set; }

        [Column("codigo")]
        [StringLength(20)]
        public string Codigo { get; set; }

        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Column("descripcion")]
        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }
}