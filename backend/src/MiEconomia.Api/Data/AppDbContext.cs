using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Subcategoria> Subcategorias => Set<Subcategoria>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Subcuenta> Subcuentas => Set<Subcuenta>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>(e =>
        {
            e.Property(u => u.Nombre).HasMaxLength(100);
        });

        builder.Entity<Categoria>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(50).IsRequired();
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(10);
            e.Property(c => c.Color).HasMaxLength(7);
            e.Property(c => c.Icono).HasMaxLength(50);

            // Un usuario no puede repetir el nombre de una categoría del mismo tipo.
            e.HasIndex(c => new { c.UsuarioId, c.Tipo, c.Nombre }).IsUnique();

            e.HasOne(c => c.Usuario)
                .WithMany(u => u.Categorias)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Subcategoria>(e =>
        {
            e.Property(s => s.Nombre).HasMaxLength(50).IsRequired();

            // No se repite el nombre dentro de la misma categoría.
            e.HasIndex(s => new { s.CategoriaId, s.Nombre }).IsUnique();

            e.HasOne(s => s.Categoria)
                .WithMany(c => c.Subcategorias)
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Movimiento>(e =>
        {
            e.Property(m => m.Monto).HasPrecision(18, 2);
            e.Property(m => m.Nota).HasMaxLength(250);
            e.ToTable(t => t.HasCheckConstraint("CK_Movimientos_Monto_Positivo", "\"Monto\" > 0"));

            e.HasIndex(m => new { m.UsuarioId, m.Fecha });

            e.HasOne(m => m.Usuario)
                .WithMany(u => u.Movimientos)
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // No se puede borrar una categoría que todavía tiene movimientos.
            e.HasOne(m => m.Categoria)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(m => m.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Al borrar una subcategoría, sus movimientos quedan en la categoría sin subcategoría.
            e.HasOne(m => m.Subcategoria)
                .WithMany(s => s.Movimientos)
                .HasForeignKey(m => m.SubcategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Lo mismo con las cuentas y las subcuentas.
            e.HasOne(m => m.Cuenta)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(m => m.CuentaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Subcuenta)
                .WithMany()
                .HasForeignKey(m => m.SubcuentaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Cuenta>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(50).IsRequired();
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Color).HasMaxLength(7);

            e.HasIndex(c => new { c.UsuarioId, c.Nombre }).IsUnique();

            e.HasOne(c => c.Usuario)
                .WithMany(u => u.Cuentas)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Subcuenta>(e =>
        {
            e.Property(s => s.Nombre).HasMaxLength(50).IsRequired();
            e.Property(s => s.Moneda).HasMaxLength(3).IsFixedLength().IsRequired();
            e.Property(s => s.SaldoInicial).HasPrecision(18, 2);

            // No se repite el nombre dentro de la misma cuenta.
            e.HasIndex(s => new { s.CuentaId, s.Nombre }).IsUnique();

            // Al borrar una cuenta (solo se puede si no tiene movimientos) se borran sus subcuentas.
            e.HasOne(s => s.Cuenta)
                .WithMany(c => c.Subcuentas)
                .HasForeignKey(s => s.CuentaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transferencia>(e =>
        {
            e.Property(t => t.Monto).HasPrecision(18, 2);
            e.Property(t => t.MontoDestino).HasPrecision(18, 2);
            e.Property(t => t.Nota).HasMaxLength(250);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Transferencias_Monto_Positivo", "\"Monto\" > 0");
                t.HasCheckConstraint("CK_Transferencias_MontoDestino_Positivo", "\"MontoDestino\" IS NULL OR \"MontoDestino\" > 0");
                // Puede ser la misma cuenta (por ejemplo, del día a día al ahorro), pero no la misma subcuenta.
                t.HasCheckConstraint("CK_Transferencias_Subcuentas_Distintas", "\"SubcuentaOrigenId\" <> \"SubcuentaDestinoId\"");
            });

            e.HasIndex(t => new { t.UsuarioId, t.Fecha });

            e.HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // No se puede borrar una cuenta que todavía tiene transferencias.
            e.HasOne(t => t.CuentaOrigen)
                .WithMany()
                .HasForeignKey(t => t.CuentaOrigenId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CuentaDestino)
                .WithMany()
                .HasForeignKey(t => t.CuentaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.SubcuentaOrigen)
                .WithMany()
                .HasForeignKey(t => t.SubcuentaOrigenId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.SubcuentaDestino)
                .WithMany()
                .HasForeignKey(t => t.SubcuentaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
