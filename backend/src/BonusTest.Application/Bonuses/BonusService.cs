
using System.Globalization;
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository)
{
    private const string ActiveEmployee = "0101";
    private const string Registered = "0201";
    private const string Cancelled = "0203";

    public Task<PagedResponse<BonusDto>> GetPagedAsync(
        BonusFilterRequest request,
        CancellationToken cancellationToken)
    {
        return repository.GetPagedAsync(request, cancellationToken);
    }

    public async Task<BonusEmployeeDto> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        var employee = await repository.GetEmployeeAsync(
            departmentCode, employeeNumber, cancellationToken);

        if (employee is null)
            throw new BusinessException(
                "EMPLOYEE_NOT_FOUND",
                "El empleado no existe.");

        if (employee.EmployeeStateCode != ActiveEmployee)
            throw new BusinessException(
                "EMPLOYEE_INACTIVE",
                "El empleado no se encuentra activo.");

        return new BonusEmployeeDto(
            employee.DepartmentCode,
            employee.EmployeeNumber,
            employee.FullName,
            employee.HireDate,
            employee.MonthlySalary,
            employee.TaxRate);
    }

    public async Task<CreateBonusResponse> CreateAsync(
        CreateBonusRequest request,
        string userCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userCode))
            throw new BusinessException(
                "USER_REQUIRED",
                "El usuario es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.DepartmentCode)
            || request.EmployeeNumber <= 0)
            throw new BusinessException(
                "INVALID_EMPLOYEE",
                "Debe indicar un empleado válido.");

        if (!DateTime.TryParseExact(
                request.Period,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var period))
            throw new BusinessException(
                "INVALID_PERIOD",
                "El periodo debe tener formato YYYY-MM.");

        var currentMonth = new DateTime(
            DateTime.Today.Year, DateTime.Today.Month, 1);

        if (period > currentMonth)
            throw new BusinessException(
                "FUTURE_PERIOD",
                "El periodo no puede ser posterior al mes actual.");

        var employee = await repository.GetEmployeeAsync(
            request.DepartmentCode,
            request.EmployeeNumber,
            cancellationToken);

        if (employee is null)
            throw new BusinessException(
                "EMPLOYEE_NOT_FOUND",
                "El empleado no existe.");

        if (employee.EmployeeStateCode != ActiveEmployee)
            throw new BusinessException(
                "EMPLOYEE_INACTIVE",
                "El empleado no se encuentra activo.");

        if (request.BonusTypeId is not (1 or 2))
            throw new BusinessException(
                "INVALID_BONUS_TYPE",
                "El tipo de bono no es válido.");

        if (request.BonusValue <= 0)
            throw new BusinessException(
                "INVALID_BONUS_VALUE",
                "El valor del bono debe ser mayor que cero.");

        if (request.BonusTypeId == 1
            && request.BonusValue > employee.MonthlySalary)
            throw new BusinessException(
                "BONUS_EXCEEDS_SALARY",
                "El bono fijo no puede superar el sueldo mensual.");

        if (request.BonusTypeId == 2
            && request.BonusValue > 100)
            throw new BusinessException(
                "INVALID_PERCENTAGE",
                "El porcentaje debe ser mayor que cero y hasta 100.");

        if (!await repository.IsActiveReasonAsync(
                request.ReasonCode, cancellationToken))
            throw new BusinessException(
                "INVALID_REASON",
                "El motivo no existe o está inactivo.");

        if (await repository.ExistsActiveBonusAsync(
                request.DepartmentCode,
                request.EmployeeNumber,
                request.Period,
                cancellationToken))
            throw new BusinessException(
                "DUPLICATE_BONUS",
                "El empleado ya tiene un bono activo en este periodo.");

        decimal Round(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        var gross = request.BonusTypeId == 1
            ? Round(request.BonusValue)
            : Round(employee.MonthlySalary * request.BonusValue / 100m);

        var tax = Round(gross * employee.TaxRate / 100m);
        var net = Round(gross - tax);

        var bonus = new Bonus
        {
            DepartmentCode = employee.DepartmentCode,
            EmployeeNumber = employee.EmployeeNumber,
            Period = request.Period,
            BonusTypeId = request.BonusTypeId,
            BonusValue = request.BonusValue,
            ReasonCode = request.ReasonCode,
            GrossAmount = gross,
            TaxAmount = tax,
            NetAmount = net,
            StateCode = Registered,
            IsDeleted = false,
            CreatedBy = userCode.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(bonus, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateBonusResponse(
            bonus.BonusId,
            gross,
            tax,
            net);
    }

    public async Task CancelAsync(
        int bonusId,
        string userCode,
        CancellationToken cancellationToken)
    {
        var bonus = await GetRegisteredBonusAsync(
            bonusId, cancellationToken);

        ValidateUser(userCode);

        bonus.StateCode = Cancelled;
        bonus.ModifiedBy = userCode.Trim();
        bonus.ModifiedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int bonusId,
        string userCode,
        CancellationToken cancellationToken)
    {
        var bonus = await GetRegisteredBonusAsync(
            bonusId, cancellationToken);

        ValidateUser(userCode);

        bonus.IsDeleted = true;
        bonus.ModifiedBy = userCode.Trim();
        bonus.ModifiedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Bonus> GetRegisteredBonusAsync(
        int bonusId,
        CancellationToken cancellationToken)
    {
        var bonus = await repository.GetByIdAsync(
            bonusId, cancellationToken);

        if (bonus is null)
            throw new BusinessException(
                "BONUS_NOT_FOUND",
                "El bono no existe.");

        if (bonus.StateCode != Registered)
            throw new BusinessException(
                "INVALID_BONUS_STATE",
                "Solo se pueden modificar bonos registrados.");

        return bonus;
    }

    private static void ValidateUser(string userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode))
            throw new BusinessException(
                "USER_REQUIRED",
                "El usuario es obligatorio.");
    }

    public async Task PayAsync(
        int bonusId,
        string userCode,
        CancellationToken cancellationToken)
    {
        ValidateUser(userCode);

        var bonus = await repository.GetByIdAsync(
            bonusId, cancellationToken);

        if (bonus is null)
        {
            throw new BusinessException(
                "BONUS_NOT_FOUND",
                "El bono no existe.");
        }

        if (bonus.StateCode != "0201")
        {
            throw new BusinessException(
                "INVALID_BONUS_STATE",
                "Solo se pueden pagar bonos registrados.");
        }

        var projects = await repository.GetEmployeeProjectHoursAsync(
            bonus.DepartmentCode,
            bonus.EmployeeNumber,
            cancellationToken);

        if (projects.Count == 0)
        {
            throw new BusinessException(
                "PROJECTS_NOT_FOUND",
                "El empleado no tiene proyectos registrados.");
        }

        if (projects.Any(x => x.Hours <= 0))
        {
            throw new BusinessException(
                "INVALID_PROJECT_HOURS",
                "Todos los proyectos deben tener horas mayores que cero.");
        }

        var totalHours = projects.Sum(x => x.Hours);

        var distributions = new List<BonusProjectDistribution>();
        decimal accumulated = 0m;

        for (var i = 0; i < projects.Count; i++)
        {
            var project = projects[i];
            var isLast = i == projects.Count - 1;

            decimal remaining = bonus.GrossAmount - accumulated;

            decimal amount = isLast
                ? remaining
                : Math.Min(
                    remaining,
                    Math.Round(
                        bonus.GrossAmount * project.Hours / totalHours,
                        2,
                        MidpointRounding.AwayFromZero));


            distributions.Add(new BonusProjectDistribution
            {
                BonusId = bonus.BonusId,
                EmployeeProjectHoursId = project.EmployeeProjectHoursId,
                ProjectName = project.ProjectName,
                Hours = project.Hours,
                DistributedAmount = amount
            });

            accumulated += amount;
        }

        bonus.ModifiedBy = userCode.Trim();
        bonus.ModifiedAt = DateTime.UtcNow;

        await repository.PayBonusAsync(
            bonus,
            distributions,
            cancellationToken);
    }

}
