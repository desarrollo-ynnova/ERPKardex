using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("asiento_contable")]
    public class AsientoContable
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("periodo_id")]
        public int PeriodoId { get; set; }

        [Column("origen_asiento_id")]
        public int OrigenAsientoId { get; set; }

        [Column("numero_asiento")]
        public string NumeroAsiento { get; set; } = string.Empty;

        [Column("fecha_contable")]
        public DateTime FechaContable { get; set; }

        [Column("moneda_id")]
        public int MonedaId { get; set; }

        [Column("tipo_cambio")]
        public decimal TipoCambio { get; set; } = 1.000000m;

        [Column("glosa")]
        public string Glosa { get; set; } = string.Empty;

        [Column("total_debe")]
        public decimal TotalDebe { get; set; }

        [Column("total_haber")]
        public decimal TotalHaber { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "REGISTRADO";

        [Column("id_referencia")]
        public int? IdReferencia { get; set; }

        [Column("tabla_referencia")]
        public string? TablaReferencia { get; set; }

        [Column("usuario_registro")]
        public int? UsuarioRegistro { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }

        // Navegación a detalles (No se mapea como columna en la BD)
        public virtual ICollection<DasientoContable> Detalles { get; set; } = new List<DasientoContable>();
    }
}