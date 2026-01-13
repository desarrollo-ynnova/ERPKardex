using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo")]
    public class Activo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo_interno")]
        public string? CodigoInterno { get; set; }

        // Clasificación
        [Column("grupo_id")]
        public int? GrupoId { get; set; }

        [Column("tipo_id")]
        public int? TipoId { get; set; }

        [Column("marca_id")]
        public int? MarcaId { get; set; }

        [Column("modelo_id")]
        public int? ModeloId { get; set; }

        // Identificación y Estado
        [Column("serie")]
        public string? Serie { get; set; }

        [Column("condicion")]
        public string? Condicion { get; set; } // OPERATIVO, MALOGRADO

        [Column("situacion")]
        public string? Situacion { get; set; } // EN USO, EN STOCK

        // Ubicación Administrativa
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("sucursal_id")]
        public int? SucursalId { get; set; }

        // Auditoría
        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}