using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("tipo_documento_identidad")]
    public class TipoDocumentoIdentidad
    {
        [Key]
        public int Id { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; } // 'RUC', 'DNI', etc.

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("longitud")]
        public int? Longitud { get; set; }

        [Column("es_alfanumerico")]
        public bool? EsAlfanumerico { get; set; } // false: solo números, true: letras y números

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}