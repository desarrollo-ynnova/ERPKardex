using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPKardex.Models
{
    [Table("pais")]
    public class Pais
    {
        [Key]
        public int Id { get; set; }
        [Column("nombre")]
        public string? Nombre { get; set; }
        [Column("name")]
        public string? Name { get; set; }
        [Column("nom")]
        public string? Nom { get; set; }
        [Column("iso2")]
        public string? Iso2 { get; set; }
        [Column("iso3")]
        public string? Iso3 { get; set; }
        [Column("phone_code")]
        public string? PhoneCode { get; set; }
        [Column("continente")]
        public string? Continente { get; set; }
        [Column("estado")]
        public bool? Estado { get; set; }
    }
}