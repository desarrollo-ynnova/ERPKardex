using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("activo")]
    public class Activo
    {
        [Key]
        public int Id { get; set; }

        [Column("codigo")]
        [StringLength(30)]
        public string Codigo { get; set; }

        [Column("tipo_activo_id")]
        public int TipoActivoId { get; set; }

        [Column("grupo_activo_id")]
        public int? GrupoActivoId { get; set; }

        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("descripcion")]
        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Column("marca")]
        [StringLength(100)]
        public string? Marca { get; set; }

        [Column("modelo")]
        [StringLength(100)]
        public string? Modelo { get; set; }

        [Column("numero_serie")]
        [StringLength(100)]
        public string? NumeroSerie { get; set; }

        [Column("placa")]
        [StringLength(20)]
        public string? Placa { get; set; }

        [Column("subtipo")]
        [StringLength(50)]
        public string? Subtipo { get; set; }

        [Column("anio_fabricacion")]
        public int? AnioFabricacion { get; set; }

        [Column("estado_uso")]
        [StringLength(20)]
        public string EstadoUso { get; set; }

        [Column("condicion")]
        [StringLength(20)]
        public string? Condicion { get; set; }

        [Column("estado")]
        public bool Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }
    }
}