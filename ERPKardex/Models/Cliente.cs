using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("cliente")]
    public class Cliente
    {
        [Key] // Es buena práctica marcar la llave primaria
        public int Id { get; set; }

        public string? Ruc { get; set; }

        [Column("razon_social")]
        public string? RazonSocial { get; set; }

        [Column("nombre_contacto")]
        public string? NombreContacto { get; set; }

        public string? Telefono { get; set; }
        public string? Email { get; set; }

        // --- CAMPOS NUEVOS ---
        [Column("banco_id")]
        public int? BancoId { get; set; }

        [Column("numero_cuenta")]
        public string? NumeroCuenta { get; set; }

        [Column("numero_detraccion")]
        public string? NumeroDetraccion { get; set; }

        [Column("numero_cci")]
        public string? NumeroCci { get; set; }
        // ---------------------

        public bool? Estado { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }
    }
}