using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
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

            // Lo mismo con las cuentas.
            e.HasOne(m => m.Cuenta)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(m => m.CuentaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Cuenta>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(50).IsRequired();
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.SaldoInicial).HasPrecision(18, 2);
            e.Property(c => c.Color).HasMaxLength(7);

            e.HasIndex(c => new { c.UsuarioId, c.Nombre }).IsUnique();

            e.HasOne(c => c.Usuario)
                .WithMany(u => u.Cuentas)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transferencia>(e =>
        {
            e.Property(t => t.Monto).HasPrecision(18, 2);
            e.Property(t => t.Nota).HasMaxLength(250);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Transferencias_Monto_Positivo", "\"Monto\" > 0");
                t.HasCheckConstraint("CK_Transferencias_Cuentas_Distintas", "\"CuentaOrigenId\" <> \"CuentaDestinoId\"");
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
        });
    }
}
