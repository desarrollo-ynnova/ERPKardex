using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("cliente")]
    public class Cliente
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Ruc { get; set; }
        [Column("razon_social")]
        public string? RazonSocial { get; set; }
        public bool? Estado { get; set; }
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}
