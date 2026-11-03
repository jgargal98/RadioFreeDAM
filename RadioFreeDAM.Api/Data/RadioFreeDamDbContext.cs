using Microsoft.EntityFrameworkCore;
using RadioFreeDAM.Api.Models;

namespace RadioFreeDAM.Api.Data;

/// <summary>
/// Acceso a la base de datos MariaDB con Entity Framework Core.
/// Las tablas llevan el prefijo rfd_ porque la base de datos se comparte con otro proyecto.
/// </summary>
public class RadioFreeDamDbContext(DbContextOptions<RadioFreeDamDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("rfd_users");
            user.Property(u => u.Name).HasMaxLength(60).IsRequired();
            user.Property(u => u.Email).HasMaxLength(254).IsRequired();
            user.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();

            // Regla del PDF: el correo es único.
            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Favorite>(favorite =>
        {
            favorite.ToTable("rfd_favorites");
            favorite.Property(f => f.StationId).HasMaxLength(64).IsRequired();

            // Regla del PDF: una favorita pertenece a un usuario existente (clave foránea).
            // Si se borra el usuario, se borran sus favoritas.
            favorite.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Regla del PDF: la misma emisora no puede estar dos veces en las favoritas de un usuario.
            favorite.HasIndex(f => new { f.UserId, f.StationId }).IsUnique();
        });
    }
}
