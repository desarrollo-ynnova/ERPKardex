using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("Centro_costo")]
    public class CentroCosto
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("padre_id")]
        public int? PadreId { get; set; }

        [Column("es_imputable")]
        public bool? EsImputable { get; set; }

        // --- NUEVOS CAMPOS ---
        [Column("fecha_inicio")]
        public DateTime? FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateTime? FechaFin { get; set; }

        [Column("tipo_cuenta_id")]
        public int? TipoCuentaId { get; set; }

        [Column("cuenta_cargo")]
        public string? CuentaCargo { get; set; }

        [Column("cuenta_abono")]
        public string? CuentaAbono { get; set; }
        // ---------------------

        public bool? Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }
}