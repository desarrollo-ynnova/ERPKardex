namespace ERPKardex.ViewModels
{
    public class ActivoListadoViewModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Tipo { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Serie { get; set; }
        public string Condicion { get; set; }
        public string Situacion { get; set; }
        public string AsignadoA { get; set; } // Nombre Persona o Área
        public string Ubicacion { get; set; }
    }

    public class HistorialAsignacionViewModel
    {
        public int Id { get; set; }
        public string Responsable { get; set; } // Persona o Área
        public string FechaInicio { get; set; }
        public string FechaFin { get; set; }
        public string Estado { get; set; } // Vigente / Histórico
        public string Observacion { get; set; }
        public string RutaActa { get; set; }
    }
}