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
        public DbSet<Entidad> Entidades { get; set; }
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
        public DbSet<Personal> Personal { get; set; }

    }
}