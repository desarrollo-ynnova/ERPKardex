using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("vehiculo_gps")]
    public class VehiculoGps
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; }

        [Column("proveedor_url")]
        public string? ProveedorUrl { get; set; }

        [Column("usuario")]
        public string? Usuario { get; set; }

        [Column("contrasena")]
        public string? Contrasena { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}