using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("cuenta_contable")]
    public class CuentaContable
    {
        [Key]
        [Column("id")] public int Id { get; set; }
        [Column("codigo")] public string Codigo { get; set; } = string.Empty;
        [Column("nombre")] public string Nombre { get; set; } = string.Empty;
        [Column("padre_id")] public int? PadreId { get; set; }
        [Column("nivel")] public int? Nivel { get; set; }
        [Column("tipo_elemento")] public string? TipoElemento { get; set; }
        [Column("es_movimiento")] public bool? EsMovimiento { get; set; }
        [Column("empresa_id")] public int? EmpresaId { get; set; }
        [Column("estado")] public bool? Estado { get; set; }
    }
}