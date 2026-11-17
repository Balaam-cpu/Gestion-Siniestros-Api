using GestionSiniestros.Api.Dtos;
using GestionSiniestros.Api.Interfaces;
using GestionSiniestros.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionSiniestros.Api.Controllers;

[ApiController]
[Route("claims")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClaimResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ClaimResponse>>> GetAll([FromQuery] string? policyNumber, [FromQuery] ClaimStatus? status, CancellationToken cancellationToken)
    {
        var claims = await claimService.GetAllAsync(policyNumber, status, cancellationToken);
        return Ok(claims);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var claim = await claimService.GetByIdAsync(id, cancellationToken);
        return claim is null ? NotFound() : Ok(claim);
    }

    [HttpPost]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponse>> Create(SaveClaimRequest request, CancellationToken cancellationToken)
    {
        var created = await claimService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> Update(int id, SaveClaimRequest request, CancellationToken cancellationToken)
    {
        var updated = await claimService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> ChangeStatus(int id, ChangeClaimStatusRequest request, CancellationToken cancellationToken)
    {
        var (outcome, claim) = await claimService.ChangeStatusAsync(id, request.Status!.Value, cancellationToken);

        switch (outcome)
        {
            case ChangeStatusOutcome.NotFound:
                return NotFound();
            case ChangeStatusOutcome.SameStatus:
                ModelState.AddModelError("status","El siniestro ya se encuentra en ese estado.");
                return ValidationProblem(ModelState);
            default:
                return Ok(claim);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await claimService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/status-history")]
    [ProducesResponseType<IReadOnlyList<ClaimStatusHistoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClaimStatusHistoryResponse>>> GetStatusHistory(int id, CancellationToken cancellationToken)
    {
        var history = await claimService.GetStatusHistoryAsync(id, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }
}
