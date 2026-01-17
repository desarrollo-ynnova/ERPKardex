using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("ddocumento_pagar")]
    public class DDocumentoPagar
    {
        public int Id { get; set; }
        public int DocumentoPagarId { get; set; }
        public int Item { get; set; }

        public string? TablaOrigen { get; set; } // 'DORDENCOMPRA'
        public int? OrigenId { get; set; }      // ID de dordencompra

        public string? Descripcion { get; set; }
        public string? UnidadMedida { get; set; }

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }

        public int? CentroCostoId { get; set; }
        public string? CuentaContable { get; set; }
    }
}