
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BonusManagement.Tests.Integration;

public class BonusPaymentConcurrencyTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PayBonusAsync_ConcurrentRequests_OnlyOneSucceeds()
    {
        await using var setupContext =
            IntegrationDbContextFactory.Create();

        var employee = await setupContext.Employees
            .AsNoTracking()
            .FirstAsync();

        var reason = await setupContext.BonusReasons
            .AsNoTracking()
            .FirstAsync();

        var bonusType = await setupContext.BonusTypes
            .AsNoTracking()
            .FirstAsync();

        var bonus = new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = "2026-11",
            BonusTypeId = bonusType.BonusTypeId,
            BonusValue = 100m,
            ReasonCode = reason.ReasonCode,
            GrossAmount = 100m,
            TaxAmount = 8m,
            NetAmount = 92m,
            StateCode = "0201",
            IsDeleted = false,
            CreatedBy = "concurrency-test",
            CreatedAt = DateTime.UtcNow
        };

        setupContext.Bonuses.Add(bonus);
        await setupContext.SaveChangesAsync();

        try
        {
            async Task<Exception?> AttemptPaymentAsync()
            {
                await using var context =
                    IntegrationDbContextFactory.Create();

                var repository = new BonusRepository(context);

                var paymentBonus = new Bonus
                {
                    BonusId = bonus.BonusId,
                    ModifiedBy = "concurrency-test",
                    ModifiedAt = DateTime.UtcNow
                };

                var distributions = new List<BonusProjectDistribution>
                {
                    new()
                    {
                        BonusId = bonus.BonusId,
                        EmployeeProjectHoursId = 1,
                        ProjectName = "Proyecto A",
                        Hours = 75m,
                        DistributedAmount = 75m
                    },
                    new()
                    {
                        BonusId = bonus.BonusId,
                        EmployeeProjectHoursId = 2,
                        ProjectName = "Proyecto B",
                        Hours = 25m,
                        DistributedAmount = 25m
                    }
                };

                try
                {
                    await repository.PayBonusAsync(
                        paymentBonus,
                        distributions,
                        CancellationToken.None);

                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            var results = await Task.WhenAll(
                Task.Run(AttemptPaymentAsync),
                Task.Run(AttemptPaymentAsync));

            Assert.Single(results, result => result is null);

            await using var verificationContext =
                IntegrationDbContextFactory.Create();

            var savedBonus = await verificationContext.Bonuses
                .AsNoTracking()
                .SingleAsync(x => x.BonusId == bonus.BonusId);

            var savedDistributions =
                await verificationContext.BonusProjectDistributions
                    .AsNoTracking()
                    .Where(x => x.BonusId == bonus.BonusId)
                    .ToListAsync();

            Assert.Equal("0202", savedBonus.StateCode);
            Assert.Equal(2, savedDistributions.Count);
            Assert.Equal(
                100m,
                savedDistributions.Sum(x => x.DistributedAmount));
        }
        finally
        {
            await setupContext.BonusProjectDistributions
                .Where(x => x.BonusId == bonus.BonusId)
                .ExecuteDeleteAsync();

            await setupContext.Bonuses
                .Where(x =>
                    x.BonusId == bonus.BonusId &&
                    x.CreatedBy == "concurrency-test")
                .ExecuteDeleteAsync();
        }
    }
}
