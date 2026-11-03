
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BonusManagement.Tests.Integration;

public class DatabaseConnectionTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DatabaseConnection_ShouldConnectToIntegrationDatabase()
    {
        // Arrange
        await using var context = IntegrationDbContextFactory.Create();

        // Act
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        Assert.True(
            canConnect,
            "No fue posible conectar con SQL Server.");

        var databaseName = context.Database
            .GetDbConnection()
            .Database;

        Assert.Equal(
            "BonusTest_Integration",
            databaseName);

        // Verificar que las tres tablas existen.
        var tableNames = new[]
        {
            "Bonus",
            "EmployeeProjectHours",
            "BonusProjectDistribution"
        };

        foreach (var tableName in tableNames)
        {
            var connection = context.Database.GetDbConnection();

            await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();

                command.CommandText = """
                    SELECT COUNT(*)
                    FROM sys.tables
                    WHERE name = @tableName
                      AND schema_id = SCHEMA_ID('dbo')
                    """;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;

                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();

                Assert.Equal(1, Convert.ToInt32(result));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
