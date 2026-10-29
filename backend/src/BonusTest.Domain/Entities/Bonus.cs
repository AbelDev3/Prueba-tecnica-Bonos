
namespace BonusTest.Domain.Entities;

public class Bonus
{
    public int BonusId { get; set; }

    public string DepartmentCode { get; set; } = null!;

    public int EmployeeNumber { get; set; }

    public string Period { get; set; } = null!;

    public int BonusTypeId { get; set; }

    public decimal BonusValue { get; set; }

    public string ReasonCode { get; set; } = null!;

    public decimal GrossAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal NetAmount { get; set; }

    public string StateCode { get; set; } = "0201";

    public bool IsDeleted { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    // Navegación hacia el empleado
    public Employee Employee { get; set; } = null!;
}
