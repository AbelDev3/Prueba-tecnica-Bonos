
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    public Task<Employee?> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        return context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DepartmentCode == departmentCode
                  && x.EmployeeNumber == employeeNumber,
                cancellationToken);
    }

    public async Task<PagedResponse<BonusDto>> GetPagedAsync(
        BonusFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.Bonuses
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            query = query.Where(
                x => x.DepartmentCode == request.DepartmentCode);
        }

        if (request.EmployeeNumber.HasValue)
        {
            query = query.Where(
                x => x.EmployeeNumber == request.EmployeeNumber.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Period))
        {
            query = query.Where(
                x => x.Period == request.Period);
        }

        if (!string.IsNullOrWhiteSpace(request.StateCode))
        {
            query = query.Where(
                x => x.StateCode == request.StateCode);
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await (
            from bonus in query
            join employee in context.Employees
                on new { bonus.DepartmentCode, bonus.EmployeeNumber }
                equals new { employee.DepartmentCode, employee.EmployeeNumber }
            join department in context.Departments
                on bonus.DepartmentCode equals department.DepartmentCode
            join bonusType in context.BonusTypes
                on bonus.BonusTypeId equals bonusType.BonusTypeId
            join reason in context.BonusReasons
                on bonus.ReasonCode equals reason.ReasonCode
            join state in context.States
                on bonus.StateCode equals state.StateCode
            orderby bonus.BonusId descending
            select new BonusDto(
                bonus.BonusId,
                bonus.DepartmentCode,
                department.DepartmentName,
                bonus.EmployeeNumber,
                employee.FullName,
                bonus.Period,
                bonus.BonusTypeId,
                bonusType.BonusTypeDescription,
                bonus.BonusValue,
                bonus.ReasonCode,
                reason.ReasonDescription,
                bonus.GrossAmount,
                bonus.TaxAmount,
                bonus.NetAmount,
                bonus.StateCode,
                state.StateDescription
            )
        )
        .Skip((request.PageIndex - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        return PagedResponse<BonusDto>.Create(
            data,
            request.PageIndex,
            request.PageSize,
            totalRecords);
    }

    public Task<Bonus?> GetByIdAsync(
        int bonusId,
        CancellationToken cancellationToken)
    {
        return context.Bonuses
            .FirstOrDefaultAsync(
                x => x.BonusId == bonusId && !x.IsDeleted,
                cancellationToken);
    }

    public Task<bool> ExistsActiveBonusAsync(
        string departmentCode,
        int employeeNumber,
        string period,
        CancellationToken cancellationToken)
    {
        return context.Bonuses.AnyAsync(
            x => x.DepartmentCode == departmentCode
              && x.EmployeeNumber == employeeNumber
              && x.Period == period
              && !x.IsDeleted
              && x.StateCode != "0203",
            cancellationToken);
    }

    public Task<bool> IsActiveReasonAsync(
        string reasonCode,
        CancellationToken cancellationToken)
    {
        return context.BonusReasons.AnyAsync(
            x => x.ReasonCode == reasonCode && x.State,
            cancellationToken);
    }

    public async Task AddAsync(
        Bonus bonus,
        CancellationToken cancellationToken)
    {
        await context.Bonuses.AddAsync(bonus, cancellationToken);
    }

    public Task<List<EmployeeProjectHours>> GetEmployeeProjectHoursAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        return context.EmployeeProjectHours
            .AsNoTracking()
            .Where(x =>
                x.DepartmentCode == departmentCode &&
                x.EmployeeNumber == employeeNumber)
            .OrderBy(x => x.EmployeeProjectHoursId)
            .ToListAsync(cancellationToken);
    }

    public async Task PayBonusAsync(
        Bonus bonus,
        IReadOnlyCollection<BonusProjectDistribution> distributions,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Actualización condicional para impedir pagos duplicados.
            var updatedRows = await context.Bonuses
                .Where(x =>
                    x.BonusId == bonus.BonusId &&
                    !x.IsDeleted &&
                    x.StateCode == "0201")
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.StateCode, "0202")
                        .SetProperty(x => x.ModifiedBy, bonus.ModifiedBy)
                        .SetProperty(x => x.ModifiedAt, bonus.ModifiedAt),
                    cancellationToken);

            if (updatedRows != 1)
            {
                throw new BusinessException(
                    "INVALID_BONUS_STATE",
                    "El bono ya no está disponible para pago.");
            }

            await context.BonusProjectDistributions.AddRangeAsync(
                distributions,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
