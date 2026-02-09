using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("gps_vehiculo")]
    public class GpsVehiculo
    {
        [Key]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("empresa_gps")]
        [StringLength(200)]
        public string? EmpresaGps { get; set; }

        [Column("url_acceso")]
        [StringLength(500)]
        public string? UrlAcceso { get; set; }

        [Column("usuario")]
        [StringLength(200)]
        public string? Usuario { get; set; }

        [Column("contrasena")]
        [StringLength(200)]
        public string? Contrasena { get; set; }

        [Column("fecha_vencimiento", TypeName = "date")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("constancia")]
        [StringLength(20)]
        public string? Constancia { get; set; }

        [Column("endoso")]
        [StringLength(100)]
        public string? Endoso { get; set; }

        [Column("observacion")]
        [StringLength(500)]
        public string? Observacion { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }
    }
}