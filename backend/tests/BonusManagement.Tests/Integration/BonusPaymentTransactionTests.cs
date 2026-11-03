
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BonusManagement.Tests.Integration;

public class BonusPaymentTransactionTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PayBonusAsync_WhenDistributionFails_RollsBackEverything()
    {
        await using var context = IntegrationDbContextFactory.Create();

        Assert.True(await context.Database.CanConnectAsync());

        // Confirmar nuevamente el destino antes de modificar datos.
        Assert.Equal(
            "BonusTest_Integration",
            context.Database.GetDbConnection().Database);

        var employee = await context.Employees
            .AsNoTracking()
            .FirstAsync();

        var reason = await context.BonusReasons
            .AsNoTracking()
            .FirstAsync();

        var bonusType = await context.BonusTypes
            .AsNoTracking()
            .FirstAsync();

        var bonus = new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = "2026-10",
            BonusTypeId = bonusType.BonusTypeId,
            BonusValue = 100m,
            ReasonCode = reason.ReasonCode,
            GrossAmount = 100m,
            TaxAmount = 8m,
            NetAmount = 92m,
            StateCode = "0201",
            IsDeleted = false,
            CreatedBy = "integration-test",
            CreatedAt = DateTime.UtcNow
        };

        context.Bonuses.Add(bonus);
        await context.SaveChangesAsync();

        try
        {
            var repository = new BonusRepository(context);

            bonus.ModifiedBy = "integration-test";
            bonus.ModifiedAt = DateTime.UtcNow;

            var distributions = new List<BonusProjectDistribution>
            {
                new()
                {
                    BonusId = bonus.BonusId,
                    EmployeeProjectHoursId = 1,
                    ProjectName = "Proyecto válido",
                    Hours = 10m,
                    DistributedAmount = 50m
                },
                new()
                {
                    BonusId = bonus.BonusId,
                    EmployeeProjectHoursId = 2,
                    ProjectName = new string('X', 250),
                    Hours = 10m,
                    DistributedAmount = 50m
                }
            };

            // La inserción debe fallar y revertir la transacción.
            await Assert.ThrowsAsync<DbUpdateException>(
                () => repository.PayBonusAsync(
                    bonus,
                    distributions,
                    CancellationToken.None));

            // Consultar con otro DbContext para evitar valores
            // rastreados en memoria por el contexto original.
            await using var verificationContext =
                IntegrationDbContextFactory.Create();

            var savedBonus = await verificationContext.Bonuses
                .AsNoTracking()
                .SingleAsync(x => x.BonusId == bonus.BonusId);

            var distributionCount =
                await verificationContext.BonusProjectDistributions
                    .CountAsync(x => x.BonusId == bonus.BonusId);

            Assert.Equal("0201", savedBonus.StateCode);
            Assert.Equal(0, distributionCount);
        }
        finally
        {
            // Limpieza exclusiva del bono creado por esta prueba.
            await context.BonusProjectDistributions
                .Where(x => x.BonusId == bonus.BonusId)
                .ExecuteDeleteAsync();

            await context.Bonuses
                .Where(x =>
                    x.BonusId == bonus.BonusId &&
                    x.CreatedBy == "integration-test")
                .ExecuteDeleteAsync();
        }
    }
}
