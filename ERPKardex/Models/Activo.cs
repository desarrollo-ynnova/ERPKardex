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

        // Identificación
        [Column("codigo_interno")]
        public string? CodigoInterno { get; set; } // Placa

        [Column("serie")]
        public string? Serie { get; set; } // VIN

        // Clasificación
        [Column("activo_grupo_id")]
        public int? ActivoGrupoId { get; set; }

        [Column("activo_tipo_id")]
        public int? ActivoTipoId { get; set; }

        [Column("marca_id")]
        public int? MarcaId { get; set; }

        [Column("modelo_id")]
        public int? ModeloId { get; set; }

        // Datos Operativos
        [Column("condicion")]
        public string? Condicion { get; set; }

        [Column("situacion")]
        public string? Situacion { get; set; }

        // Datos Vehiculares
        [Column("anio_fabricacion")]
        public int? AnioFabricacion { get; set; }

        [Column("color")]
        public string? Color { get; set; }

        [Column("modalidad_adquisicion")]
        public string? ModalidadAdquisicion { get; set; }

        // Mantenimiento y Kilometraje
        [Column("medida_actual")]
        public decimal MedidaActual { get; set; }

        [Column("unidad_medida_uso")]
        public string? UnidadMedidaUso { get; set; }

        [Column("prox_mantenimiento")]
        public decimal ProxMantenimiento { get; set; }

        [Column("frecuencia_mant")]
        public decimal FrecuenciaMant { get; set; }

        // Auditoría
        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("sucursal_id")]
        public int? SucursalId { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }

        [Column("estado")]
        public bool? Estado { get; set; }
    }
}