
using BonusTest.Application.Bonuses;
using Microsoft.AspNetCore.Mvc;

namespace BonusTest.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BonusController(BonusService bonusService) : ControllerBase
{
    // GET: api/v1/Bonus
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] BonusFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bonusService.GetPagedAsync(
            request, cancellationToken);

        return Ok(result);
    }

    // GET: api/v1/Bonus/employee/TECH1/101
    [HttpGet("employee/{departmentCode}/{employeeNumber:int}")]
    public async Task<IActionResult> GetEmployee(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        var result = await bonusService.GetEmployeeAsync(
            departmentCode,
            employeeNumber,
            cancellationToken);

        return Ok(result);
    }

    // POST: api/v1/Bonus
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBonusRequest request,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        var result = await bonusService.CreateAsync(
            request,
            userCode ?? string.Empty,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // PUT: api/v1/Bonus/1/cancel
    [HttpPut("{bonusId:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int bonusId,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        await bonusService.CancelAsync(
            bonusId,
            userCode ?? string.Empty,
            cancellationToken);

        return NoContent();
    }

    // DELETE: api/v1/Bonus/1
    [HttpDelete("{bonusId:int}")]
    public async Task<IActionResult> Delete(
        int bonusId,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        await bonusService.DeleteAsync(
            bonusId,
            userCode ?? string.Empty,
            cancellationToken);

        return NoContent();
    }
}
