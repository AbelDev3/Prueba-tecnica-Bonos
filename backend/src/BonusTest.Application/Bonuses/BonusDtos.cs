
namespace BonusTest.Application.Bonuses;

// Datos del empleado encontrado
public record BonusEmployeeDto(
    string DepartmentCode,
    int EmployeeNumber,
    string FullName,
    DateTime HireDate,
    decimal MonthlySalary,
    decimal TaxRate
);

// Solicitud de registro de bono
public record CreateBonusRequest(
    string DepartmentCode,
    int EmployeeNumber,
    string Period,
    int BonusTypeId,
    decimal BonusValue,
    string ReasonCode
);

// Datos que se muestran en el listado
public record BonusDto(
    int BonusId,
    string DepartmentCode,
    string DepartmentName,
    int EmployeeNumber,
    string EmployeeName,
    string Period,
    int BonusTypeId,
    string BonusTypeDescription,
    decimal BonusValue,
    string ReasonCode,
    string ReasonDescription,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount,
    string StateCode,
    string StateDescription
);

// Respuesta al registrar un bono
public record CreateBonusResponse(
    int BonusId,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount
);
