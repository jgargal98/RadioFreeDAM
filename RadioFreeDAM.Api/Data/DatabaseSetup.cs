using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RadioFreeDAM.Api.Data;

/// <summary>
/// Configuración común de EF Core para MariaDB, usada por la API y por las herramientas de migraciones.
/// </summary>
public static class DatabaseSetup
{
    public const string ConnectionStringName = "RadioFreeDAM";

    /// <summary>Tabla donde EF apunta qué migraciones se han aplicado (con prefijo, como el resto).</summary>
    public const string MigrationsHistoryTable = "rfd_ef_migrations";

    public static void Configure(DbContextOptionsBuilder options, string connectionString, string serverVersion)
    {
        options.UseMySql(
            connectionString,
            ServerVersion.Parse(serverVersion),
            mySql => mySql
                .MigrationsHistoryTable(MigrationsHistoryTable)
                // El servidor está en Internet: reintenta si la conexión falla un momento.
                .EnableRetryOnFailure(maxRetryCount: 3));
    }
}

/// <summary>
/// Lo usa "dotnet ef migrations add" para crear el contexto sin arrancar la API ni conectarse:
/// generar una migración solo necesita saber el modelo y la versión del servidor.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RadioFreeDamDbContext>
{
    public RadioFreeDamDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RadioFreeDamDbContext>();
        DatabaseSetup.Configure(options, "Server=localhost;Database=design_time", "11.8.8-mariadb");
        return new RadioFreeDamDbContext(options.Options);
    }
}
