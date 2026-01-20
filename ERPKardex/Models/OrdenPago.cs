using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("orden_pago")]
    public class OrdenPago
    {
        [Key]
        public int Id { get; set; }

        // VINCULACIÓN CON LA DEUDA (FACTURA O ANTICIPO)
        [Column("documento_pagar_id")]
        public int DocumentoPagarId { get; set; }

        // DATOS DEL PAGO
        [Column("numero")]
        public string Numero { get; set; } // Ej: OP-00001

        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [Column("moneda_id")]
        public int MonedaId { get; set; }

        [Column("tipo_cambio")]
        public decimal? TipoCambio { get; set; } // TC del día de pago

        [Column("monto_pagado")]
        public decimal? MontoPagado { get; set; } // El monto que amortiza

        // TESORERÍA
        [Column("banco_id")]
        public int? BancoId { get; set; }

        [Column("numero_operacion")]
        public string NumeroOperacion { get; set; }

        [Column("ruta_voucher")]
        public string RutaVoucher { get; set; }

        // AUDITORÍA Y ESTADO
        [Column("estado_id")]
        public int? EstadoId { get; set; }

        [Column("observacion")]
        public string Observacion { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("usuario_registro_id")]
        public int? UsuarioRegistroId { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; } = DateTime.Now;

        [Column("usuario_anulacion_id")]
        public int? UsuarioAnulacionId { get; set; }

        [Column("fecha_anulacion")]
        public DateTime? FechaAnulacion { get; set; }
    }
}