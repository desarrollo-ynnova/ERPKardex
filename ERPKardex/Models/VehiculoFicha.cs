using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("vehiculo_ficha")]
    public class VehiculoFicha
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("activo_id")]
        public int ActivoId { get; set; } // Tu enlace con la clase Activo

        // Identificación Técnica
        [Column("placa")]
        public string Placa { get; set; } // Redundancia útil para búsquedas rápidas

        [Column("marca")]
        public string? Marca { get; set; }

        [Column("modelo")]
        public string? Modelo { get; set; }

        [Column("anio_fabricacion")]
        public int? AnioFabricacion { get; set; }

        [Column("color")]
        public string? Color { get; set; }

        [Column("carroceria")]
        public string? Carroceria { get; set; } // Pick Up 4x4, SUV

        [Column("vin_chasis")]
        public string? VinChasis { get; set; }

        [Column("nro_motor")]
        public string? NroMotor { get; set; }

        [Column("tipo_combustible")]
        public string? TipoCombustible { get; set; }

        // Datos Administrativos
        [Column("sede_asignada")]
        public string? SedeAsignada { get; set; }

        [Column("area_asignada")]
        public string? AreaAsignada { get; set; }

        [Column("valor_adquisicion")]
        public decimal? ValorAdquisicion { get; set; }

        [Column("registro_municipal")]
        public string? RegistroMunicipal { get; set; }

        // Equipamiento (Flags)
        [Column("tiene_gps")]
        public bool? TieneGps { get; set; }

        [Column("tiene_polarizado")]
        public bool? TienePolarizado { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}