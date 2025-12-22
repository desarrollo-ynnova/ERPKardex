using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Almacen")]
    public class Almacen
    {
        [Key]
        public int Id { get; set; }

        public string? Codigo { get; set; }

        public string? Nombre { get; set; }

        public bool? Estado { get; set; }

        [Column("cod_sucursal")]
        public string? CodSucursal { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}