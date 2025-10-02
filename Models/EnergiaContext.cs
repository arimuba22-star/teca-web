using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SWebEnergia.Models;

public partial class EnergiaContext : DbContext
{
    public EnergiaContext()
    {
    }

    public EnergiaContext(DbContextOptions<EnergiaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alerta> Alertas { get; set; }

    public virtual DbSet<CategoriasProducto> CategoriasProductos { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Comprobante> Comprobantes { get; set; }

    public virtual DbSet<DetalleComprobante> DetalleComprobantes { get; set; }


    public virtual DbSet<DetalleVentum> DetalleVenta { get; set; }

    public virtual DbSet<GruposTecnico> GruposTecnicos { get; set; }

  

    public virtual DbSet<MarcasBateria> MarcasBaterias { get; set; }


    //public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<DetalleMantenimiento> DetalleMantenimientos { get; set; }


    public virtual DbSet<Mantenimiento> Mantenimientos { get; set; }
    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SistemasRenovable> SistemasRenovables { get; set; }

    public virtual DbSet<Tecnico> Tecnicos { get; set; }

    public virtual DbSet<TecnicosGrupo> TecnicosGrupos { get; set; }

    public virtual DbSet<TiposSistema> TiposSistemas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VwAlertasPendiente> VwAlertasPendientes { get; set; }

    public virtual DbSet<VwHistorialMantenimiento> VwHistorialMantenimientos { get; set; }

    public virtual DbSet<VwMantenimientosPorCliente> VwMantenimientosPorClientes { get; set; }

    public virtual DbSet<VwPorcentajeMantenimientosCorrectivo> VwPorcentajeMantenimientosCorrectivos { get; set; }

    public virtual DbSet<VwTiempoPromedioMantenimiento> VwTiempoPromedioMantenimientos { get; set; }

    public virtual DbSet<VwTotalMantenimientosAtendido> VwTotalMantenimientosAtendidos { get; set; }

    public virtual DbSet<VwVentasPorCliente> VwVentasPorClientes { get; set; }

    public virtual DbSet<VwVentasPorTecnico> VwVentasPorTecnicos { get; set; }

   
   
    public DbSet<TipoComponente> TipoComponentes { get; set; } = null!;

    public virtual DbSet<Componente> Componentes { get; set; }
  


    public DbSet<Alerta> Alerta { get; set; } = null!;

    // COMENTADO: La cadena de conexión ahora se configura en Program.cs desde appsettings.json
    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("Server=localhost;Database=BDEnergia;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.HasKey(e => e.IdAlerta).HasName("PK__Alertas__D2CDBC4F3BC33582");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaResolucion).HasColumnType("datetime");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Tipo).HasMaxLength(50);

            entity.HasOne(d => d.IdSistemaNavigation)
                  .WithMany(p => p.Alertas)
                  .HasForeignKey(d => d.IdSistema)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Alertas_SistemasRenovables");

        });

        modelBuilder.Entity<CategoriasProducto>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK__Categori__A3C02A1074713909");

            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.HasOne(d => d.CategoriaPadreNavigation).WithMany(p => p.InverseCategoriaPadreNavigation)
                .HasForeignKey(d => d.CategoriaPadre)
                .HasConstraintName("FK_Categorias_CategoriaPadre");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente).HasName("PK__Clientes__D5946642FEAECA78");

            entity.HasIndex(e => e.Email, "UQ__Clientes__A9D10534BE78DF71").IsUnique();

            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaUltimaActualizacion).HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.Property(e => e.Telefono).HasMaxLength(15);
        });


        modelBuilder.Entity<Comprobante>(entity =>
        {
            entity.HasKey(e => e.IdComprobante).HasName("PK__Comproba__BF4686EDFD175F94");

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.Impuestos).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Tipo).HasMaxLength(20);
            entity.Property(e => e.Total).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.IdClienteNavigation)
                .WithMany(p => p.Comprobantes)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comproban__IdCli__1DB06A4F");

            entity.HasOne(d => d.IdMantenimientoNavigation)
                .WithMany(p => p.Comprobantes) // Esto depende que en Mantenimiento tengas ICollection<Comprobante> Comprobantes
                .HasForeignKey(d => d.IdMantenimiento)
                .OnDelete(DeleteBehavior.SetNull) // para permitir que el mantenimiento sea opcional
                .HasConstraintName("FK__Comproban__IdMan__1F98B2C1");
        });


        modelBuilder.Entity<DetalleComprobante>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK__DetalleC__E43646A5367EF70A");

            entity.ToTable("DetalleComprobante");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Concepto).HasMaxLength(255);
            entity.Property(e => e.Importe)
                .HasComputedColumnSql("([Cantidad]*[PrecioUnitario])", true)
                .HasColumnType("decimal(23, 4)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.IdComprobanteNavigation).WithMany(p => p.DetalleComprobantes)
                .HasForeignKey(d => d.IdComprobante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetalleCo__IdCom__22751F6C");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleComprobantes)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK__DetalleCo__IdPro__236943A5");
        });

        modelBuilder.Entity<DetalleMantenimiento>(entity =>
        {
            entity.ToTable("DetalleMantenimiento"); // coincide con tu BD y modelo

            entity.HasKey(e => e.IdDetalle);

            // propiedades
            entity.Property(e => e.Cantidad)
                .IsRequired();

            entity.Property(e => e.PrecioUnitario)
                .HasColumnType("decimal(18,2)");

            // SubTotal es calculada en el modelo (no persistida / no mapeada)
            entity.Ignore(e => e.SubTotal);

            // Relaciones
            entity.HasOne(d => d.Mantenimiento)
                .WithMany(m => m.Detalles)              // en tu Mantenimiento la colección se llama Detalles
                .HasForeignKey(d => d.IdMantenimiento)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Producto)
                .WithMany(p => p.DetalleMantenimientos) // en Producto la colección se llama DetalleMantenimientos
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleVentum>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK__DetalleV__E43646A55FC77BCA");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Importe)
                .HasComputedColumnSql("([Cantidad]*[PrecioUnitario])", true)
                .HasColumnType("decimal(23, 4)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleVenta_Productos");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleVenta_Ventas");
        });

        modelBuilder.Entity<GruposTecnico>(entity =>
        {
            entity.HasKey(e => e.IdGrupo).HasName("PK__GruposTe__303F6FD9FBDE8017");

            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.HasOne(d => d.ResponsableNavigation).WithMany(p => p.GruposTecnicos)
                .HasForeignKey(d => d.Responsable)
                .HasConstraintName("FK_Grupos_Tecnicos");
        });

        modelBuilder.Entity<Mantenimiento>(entity =>
        {
            entity.HasKey(e => e.IdMantenimiento);

            // Fechas / strings / decimales
            entity.Property(e => e.TipoMantenimiento)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");

            entity.Property(e => e.FechaSolicitud)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.FechaProgramada)
                .HasColumnType("datetime");

            entity.Property(e => e.FechaFin)
                .HasColumnType("datetime");

            entity.Property(e => e.CostoMantenimiento)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.CostoTotal)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.RequiereProductos)
                .HasDefaultValue(false);

            // Relaciones
            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.Mantenimientos)
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SistemaRenovable)
                .WithMany(s => s.Mantenimientos)
                .HasForeignKey(e => e.IdSistema)
                .OnDelete(DeleteBehavior.Restrict);

            // (Opcional) relación para navegación inversa ya cubierta por DetalleMantenimiento config
        });

        modelBuilder.Entity<MarcasBateria>(entity =>
        {
            entity.HasKey(e => e.IdMarca).HasName("PK__MarcasBa__4076A8873B3FEDBB");

            entity.Property(e => e.NecesitaMantenimiento).HasDefaultValue(false);
            entity.Property(e => e.NombreMarca).HasMaxLength(100);

            entity.HasOne(d => d.IdTipoSistemaNavigation).WithMany(p => p.MarcasBateria)
                .HasForeignKey(d => d.IdTipoSistema)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MarcasBaterias_Tipos");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK__Producto__09889210E2D0A115");

            entity.HasIndex(e => e.CodigoSku, "UQ__Producto__F02F03F9FDCE47E0").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CodigoSku)
                .HasMaxLength(50)
                .HasColumnName("CodigoSKU");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.StockMinimo).HasDefaultValue(5);
            entity.Property(e => e.UnidadMedida).HasMaxLength(20);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Categorias");

        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK__Roles__2A49584C1D69267E");

            entity.HasIndex(e => e.Nombre, "UQ__Roles__75E3EFCFABDE0CE2").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Componente>(entity =>
        {
            entity.HasKey(e => e.IdComponente);

            entity.Property(e => e.Descripcion).HasMaxLength(255);
            entity.Property(e => e.CapacidadEnergetica).HasColumnType("decimal(10,2)");
            entity.Property(e => e.FechaInstalacion).HasColumnType("date");
            entity.Property(e => e.UltimoFechaMantenimiento).HasColumnType("date");

            entity.HasOne(d => d.TipoComponente)
                  .WithMany(p => p.Componentes)
                  .HasForeignKey(d => d.IdTipoComponente)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Componentes_TipoComponentes");
        });

        modelBuilder.Entity<TipoComponente>(entity =>
        {
            entity.ToTable("TipoComponentes"); // 👈 Esta línea soluciona el error

            entity.HasKey(e => e.IdTipoComponente);

            entity.Property(e => e.Descripcion).HasMaxLength(255);
        });




        modelBuilder.Entity<SistemasRenovable>(entity =>
        {
            entity.HasKey(e => e.IdSistema).HasName("PK__Sistemas__48B026F4B2C7885A");

            entity.Property(e => e.CapacidadKw)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("CapacidadKW");

            entity.Property(e => e.Descripcion).HasMaxLength(500);

            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Activo");

            entity.HasOne(d => d.IdClienteNavigation)
                .WithMany(p => p.SistemasRenovables)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sistemas_Clientes");

            entity.HasOne(d => d.IdTipoSistemaNavigation)
                .WithMany(p => p.SistemasRenovables)
                .HasForeignKey(d => d.IdTipoSistema)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Sistemas_Tipos");

            // Relación con Componente
            


        });


        modelBuilder.Entity<Tecnico>(entity =>
        {
            entity.HasKey(e => e.IdTecnico).HasName("PK__Tecnicos__BF289893110F2DCB");

            entity.HasIndex(e => e.IdUsuario, "UQ__Tecnicos__5B65BF967536213F").IsUnique();

            entity.Property(e => e.Especialidad).HasMaxLength(100);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Tecnico)
                .HasForeignKey<Tecnico>(d => d.IdUsuario)
                .HasConstraintName("FK_Tecnicos_Usuarios");
        });

        modelBuilder.Entity<TecnicosGrupo>(entity =>
        {
            entity.HasKey(e => new { e.IdTecnico, e.IdGrupo }).HasName("PK__Tecnicos__3C2B6E6E0374FD97");

            entity.Property(e => e.FechaAsignacion).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.TecnicosGrupos)
                .HasForeignKey(d => d.IdGrupo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TecnicosGrupos_Grupo");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.TecnicosGrupos)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TecnicosGrupos_Tecnico");
        });

        modelBuilder.Entity<TiposSistema>(entity =>
        {
            entity.HasKey(e => e.IdTipoSistema).HasName("PK__TiposSis__38E6DAAFF87C6888");

            entity.ToTable("TiposSistema");

            entity.HasIndex(e => e.Nombre, "UQ__TiposSis__75E3EFCF2129AF0B").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Usuarios__5B65BF978C04935D");

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D105349AD63465").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Salt).HasMaxLength(128);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("PK__Ventas__BC1240BD0C58D5BA");

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Total).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Clientes");

            //entity.HasOne(d => d.IdMantenimientoNavigation).WithMany(p => p.Venta)
            //    .HasForeignKey(d => d.IdMantenimiento)
            //    .HasConstraintName("FK_Ventas_Mantenimientos");
        });

        modelBuilder.Entity<VwAlertasPendiente>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AlertasPendientes");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.FechaGeneracion).HasColumnType("datetime");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Tipo).HasMaxLength(50);
            entity.Property(e => e.TipoSistema).HasMaxLength(50);
        });

        modelBuilder.Entity<VwHistorialMantenimiento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_HistorialMantenimientos");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Cliente).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.FechaFin).HasColumnType("datetime");
            entity.Property(e => e.FechaInicio).HasColumnType("datetime");
            entity.Property(e => e.FechaSolicitud).HasColumnType("datetime");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Producto).HasMaxLength(100);
            entity.Property(e => e.Sistema).HasMaxLength(500);
            entity.Property(e => e.TipoMantenimiento).HasMaxLength(20);
            entity.Property(e => e.TotalProducto).HasColumnType("decimal(23, 4)");
        });

        modelBuilder.Entity<VwMantenimientosPorCliente>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MantenimientosPorCliente");

            entity.Property(e => e.Cliente).HasMaxLength(100);
            entity.Property(e => e.TotalCosto).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwPorcentajeMantenimientosCorrectivo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PorcentajeMantenimientosCorrectivos");

            entity.Property(e => e.PorcentajeCorrectivo).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<VwTiempoPromedioMantenimiento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TiempoPromedioMantenimiento");
        });

        modelBuilder.Entity<VwTotalMantenimientosAtendido>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TotalMantenimientosAtendidos");
        });

        modelBuilder.Entity<VwVentasPorCliente>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VentasPorCliente");

            entity.Property(e => e.Cliente).HasMaxLength(100);
            entity.Property(e => e.TotalFacturado).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwVentasPorTecnico>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_VentasPorTecnico");

            entity.Property(e => e.NombreTecnico).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
