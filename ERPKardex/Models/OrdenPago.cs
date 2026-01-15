using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("orden_pago")]
    public class OrdenPago
    {
        [Key]
        public int Id { get; set; }

        // Vinculación (Puede ser uno u otro)
        [Column("ordencompra_id")]
        public int? OrdenCompraId { get; set; }

        [Column("ordenservicio_id")]
        public int? OrdenServicioId { get; set; }

        // Snapshot de la Orden Original
        [Column("fecha_orden")]
        public DateTime? FechaOrden { get; set; }

        [Column("numero_orden")]
        public string? NumeroOrden { get; set; }

        [Column("moneda_orden_id")]
        public int? MonedaOrdenId { get; set; }

        [Column("monto_total_orden")]
        public decimal? MontoTotalOrden { get; set; }

        // Datos Financieros
        [Column("condicion_pago")]
        public string? CondicionPago { get; set; } // CONTADO, CREDITO

        [Column("dias_credito")]
        public int DiasCredito { get; set; }

        // Datos del Pago Real
        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [Column("tipo_cambio_pago", TypeName = "decimal(12,6)")]
        public decimal? TipoCambioPago { get; set; }

        [Column("monto_abonado")]
        public decimal MontoAbonado { get; set; }

        [Column("banco_id")]
        public int? BancoId { get; set; }

        [Column("numero_operacion")]
        public string? NumeroOperacion { get; set; }

        // Deducciones
        [Column("tiene_deduccion")]
        public bool TieneDeduccion { get; set; }

        [Column("tipo_deduccion")]
        public string? TipoDeduccion { get; set; } // DETRACCION, RETENCION

        [Column("monto_deduccion")]
        public decimal MontoDeduccion { get; set; }

        // Cálculos
        [Column("dias_retraso")]
        public int DiasRetraso { get; set; }

        // Auditoría
        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("usuario_registro_id")]
        public int? UsuarioRegistroId { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }

        [Column("estado_id")]
        public int EstadoId { get; set; } // 1: Generado, 2: Anulado, etc.

        [Column("ruta_voucher")]
        public string? RutaVoucher { get; set; }
    }
}