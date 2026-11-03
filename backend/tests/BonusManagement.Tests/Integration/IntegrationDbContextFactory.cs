
using BonusTest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace BonusManagement.Tests.Integration;

public static class IntegrationDbContextFactory
{
    public static AppDbContext Create()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.Integration.json",
                optional: false)
            .Build();

        var connectionString =
            configuration.GetConnectionString("BonusTest");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se configuró la conexión de integración.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var context = new AppDbContext(options);

        // Protección para evitar utilizar otra base de datos.
        if (!string.Equals(
            context.Database.GetDbConnection().Database,
            "BonusTest_Integration",
            StringComparison.OrdinalIgnoreCase))
        {
            context.Dispose();

            throw new InvalidOperationException(
                "Las pruebas solo pueden utilizar BonusTest_Integration.");
        }

        return context;
    }
}
