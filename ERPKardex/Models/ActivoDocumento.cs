using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo_documento")]
    public class ActivoDocumento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("tipo_documento")]
        public string? TipoDocumento { get; set; } // SOAT, REV_TECNICA

        [Column("nro_documento")]
        public string? NroDocumento { get; set; }

        [Column("fecha_emision")]
        public DateTime? FechaEmision { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("aseguradora")]
        public string? Aseguradora { get; set; }

        [Column("ruta_archivo")]
        public string? RutaArchivo { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}