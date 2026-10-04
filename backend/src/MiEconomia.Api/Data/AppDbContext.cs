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
        });
    }
}
