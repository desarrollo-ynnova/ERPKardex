using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("proveedor")]
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Column("origen_id")] public int? OrigenId { get; set; }
        [Column("tipo_persona_id")] public int? TipoPersonaId { get; set; }
        [Column("tipo_documento_identidad_id")] public int? TipoDocumentoIdentidadId { get; set; }
        [Column("numero_documento")] public string? NumeroDocumento { get; set; }

        [Column("razon_social")] public string? RazonSocial { get; set; }
        [Column("direccion")] public string? Direccion { get; set; }
        [Column("pais_id")] public int? PaisId { get; set; }
        [Column("ciudad_id")] public int? CiudadId { get; set; }

        [Column("nombre_contacto")] public string? NombreContacto { get; set; }
        [Column("cargo_contacto")] public string? CargoContacto { get; set; }
        [Column("correo_electronico")] public string? CorreoElectronico { get; set; }
        [Column("telefono")] public string? Telefono { get; set; }

        // --- DATOS BANCARIOS ---
        [Column("banco_id")] public int? BancoId { get; set; }
        [Column("codigo_swift")] public string? CodigoSwift { get; set; }

        // NUEVO CAMPO: DETRACCIONES
        [Column("numero_cuenta_detracciones")]
        public string? NumeroCuentaDetracciones { get; set; }

        // Cuentas Comerciales
        [Column("moneda_id_uno")] public int? MonedaIdUno { get; set; }
        [Column("numero_cuenta_uno")] public string? NumeroCuentaUno { get; set; }
        [Column("numero_cci_uno")] public string? NumeroCciUno { get; set; }

        [Column("moneda_id_dos")] public int? MonedaIdDos { get; set; }
        [Column("numero_cuenta_dos")] public string? NumeroCuentaDos { get; set; }
        [Column("numero_cci_dos")] public string? NumeroCciDos { get; set; }

        [Column("moneda_id_tres")] public int? MonedaIdTres { get; set; }
        [Column("numero_cuenta_tres")] public string? NumeroCuentaTres { get; set; }
        [Column("numero_cci_tres")] public string? NumeroCciTres { get; set; }

        public bool? Estado { get; set; }
        [Column("empresa_id")] public int? EmpresaId { get; set; }
        [Column("fecha_registro")] public DateTime? FechaRegistro { get; set; }
    }
}