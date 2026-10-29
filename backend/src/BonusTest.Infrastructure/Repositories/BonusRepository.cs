
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
