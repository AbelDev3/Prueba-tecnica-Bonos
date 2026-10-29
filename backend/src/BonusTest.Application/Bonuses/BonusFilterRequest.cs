
using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

public class BonusFilterRequest : PaginationRequest
{
    public string? DepartmentCode { get; set; }

    public int? EmployeeNumber { get; set; }

    public string? Period { get; set; }

    public string? StateCode { get; set; }
}
