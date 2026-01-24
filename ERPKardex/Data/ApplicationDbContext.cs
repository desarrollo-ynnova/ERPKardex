using ERPKardex.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPKardex.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<TipoUsuario> TipoUsuarios { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<EmpresaUsuario> EmpresaUsuarios { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<EmpresaUsuarioPermiso> EmpresaUsuarioPermisos { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Subgrupo> Subgrupos { get; set; }
        public DbSet<TipoExistencia> TipoExistencias { get; set; }
        public DbSet<TipoCuenta> TipoCuentas { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<UnidadMedida> UnidadesMedida { get; set; }
        public DbSet<FormulacionQuimica> FormulacionesQuimicas { get; set; }
        public DbSet<Peligrosidad> Peligrosidades { get; set; }
        public DbSet<IngredienteActivo> IngredientesActivos { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Modelo> Modelos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<DetalleIngredienteActivo> DetallesIngredientesActivos { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<StockAlmacen> StockAlmacenes { get; set; }
        public DbSet<Motivo> Motivos { get; set; }
        public DbSet<TipoDocumento> TipoDocumentos { get; set; }
        public DbSet<Moneda> Monedas { get; set; }
        public DbSet<IngresoSalidaAlm> IngresoSalidaAlms { get; set; }
        public DbSet<DIngresoSalidaAlm> DIngresoSalidaAlms { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<CentroCosto> CentroCostos { get; set; }
        public DbSet<Actividad> Actividades { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TipoDocumentoInterno> TiposDocumentoInterno { get; set; }
        public DbSet<ReqCompra> ReqCompras { get; set; }
        public DbSet<DReqCompra> DReqCompras { get; set; }
        public DbSet<ReqServicio> ReqServicios { get; set; }
        public DbSet<DReqServicio> DReqServicios { get; set; }
        public DbSet<PedCompra> PedCompras { get; set; }
        public DbSet<DPedCompra> DPedidoCompras { get; set; }
        public DbSet<PedServicio> PedServicios { get; set; }
        public DbSet<DPedServicio> DPedidosServicio { get; set; }
        public DbSet<OrdenCompra> OrdenCompras { get; set; }
        public DbSet<DOrdenCompra> DOrdenCompras { get; set; }
        public DbSet<OrdenServicio> OrdenServicios { get; set; }
        public DbSet<DOrdenServicio> DOrdenServicios { get; set; }
        public DbSet<Activo> Activos { get; set; }
        public DbSet<ActivoTipo> ActivoTipos { get; set; }
        public DbSet<ActivoGrupo> ActivoGrupos { get; set; }
        public DbSet<MovimientoActivo> MovimientosActivo { get; set; }
        public DbSet<DMovimientoActivo> DMovimientosActivo { get; set; }
        public DbSet<ActivoEspecificacion> ActivoEspecificaciones { get; set; }
        public DbSet<ActivoDocumento> ActivoDocumentos { get; set; }
        public DbSet<ActivoHistorialMedida> ActivoHistorialMedidas { get; set; }
        public DbSet<Personal> Personal { get; set; }
        public DbSet<TipoCambio> TipoCambios { get; set; }
        public DbSet<Banco> Bancos { get; set; }
        public DbSet<OrdenPago> OrdenPagos { get; set; }
        public DbSet<DocumentoPagar> DocumentosPagar { get; set; }
        public DbSet<DDocumentoPagar> DDocumentosPagar { get; set; }
        public DbSet<DocumentoPagarAplicacion> DocumentoPagarAplicaciones { get; set; }
        public DbSet<Origen> Origenes { get; set; }
        public DbSet<TipoDocumentoIdentidad> TiposDocumentoIdentidad { get; set; }
        public DbSet<TipoPersona> TiposPersona { get; set; }
        public DbSet<Pais> Paises { get; set; }
        public DbSet<Ciudad> Ciudades { get; set; }
        public DbSet<ConfiguracionGeneral> ConfiguracionesGenerales { get; set; }
        public DbSet<TipoDetraccion> TiposDetracciones { get; set; }
        public DbSet<Detraccion> Detracciones { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================================
            // CONFIGURACIÓN DE SEGURIDAD (SOLO RESTRICCIONES)
            // =================================================================

            // Restricción Única: Evitar que se asigne el mismo permiso 2 veces
            // al mismo vínculo de usuario.
            modelBuilder.Entity<EmpresaUsuarioPermiso>()
                .HasIndex(p => new { p.EmpresaUsuarioId, p.PermisoId })
                .IsUnique();

            // =================================================================
            // TIPOS DE DATOS MONETARIOS (Global)
            // =================================================================
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                // Si no se especificó un TypeName manual (como en TC), usar default 18,2
                if (property.GetColumnType() == null)
                    property.SetColumnType("decimal(18,2)");
            }
        }
    }
}