using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("entidad")]
    public class Entidad
    {
        public int Id { get; set; }
        public string? Ruc { get; set; }
        [Column("razon_social")]
        public string? RazonSocial { get; set; }
        public bool? Estado { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}
