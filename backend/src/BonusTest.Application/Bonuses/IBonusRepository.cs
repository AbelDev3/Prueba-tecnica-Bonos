
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    // Buscar empleado por clave compuesta
    Task<Employee?> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken);

    // Obtener listado paginado
    Task<PagedResponse<BonusDto>> GetPagedAsync(
        BonusFilterRequest request,
        CancellationToken cancellationToken);

    // Buscar bono por identificador
    Task<Bonus?> GetByIdAsync(
        int bonusId,
        CancellationToken cancellationToken);

    // Verificar existencia de bono activo
    Task<bool> ExistsActiveBonusAsync(
        string departmentCode,
        int employeeNumber,
        string period,
        CancellationToken cancellationToken);

    // Validar que el motivo esté activo
    Task<bool> IsActiveReasonAsync(
        string reasonCode,
        CancellationToken cancellationToken);

    // Registrar un bono
    Task AddAsync(
        Bonus bonus,
        CancellationToken cancellationToken);

    // Persistir cambios
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
