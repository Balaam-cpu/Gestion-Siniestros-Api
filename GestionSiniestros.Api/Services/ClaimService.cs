using System.Linq.Expressions;
using GestionSiniestros.Api.Data;
using GestionSiniestros.Api.Dtos;
using GestionSiniestros.Api.Interfaces;
using GestionSiniestros.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionSiniestros.Api.Services;

public class ClaimService(ClaimsDbContext dbContext) : IClaimService
{
    private static readonly Expression<Func<Claim, ClaimResponse>> ToResponseExpression = c => new ClaimResponse(
        c.Id,
        c.PolicyNumber,
        c.InsuredName,
        c.ClaimType,
        c.EstimatedAmount,
        c.Status,
        c.CreatedAt,
        c.UpdatedAt
     );

    private static readonly Func<Claim, ClaimResponse> ToResponse = ToResponseExpression.Compile();

    public async Task<IReadOnlyList<ClaimResponse>> GetAllAsync(string? policyNumber, ClaimStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Claims.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(policyNumber))
        {
            var prefix = policyNumber.Trim();
            query = query.Where(c => c.PolicyNumber.StartsWith(prefix));
        }

        if (status is ClaimStatus statusFilter)
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Select(ToResponseExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClaimResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Claims
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ClaimResponse> CreateAsync(SaveClaimRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var claim = new Claim
        {
            PolicyNumber = request.PolicyNumber!.Trim(),
            InsuredName = request.InsuredName!.Trim(),
            ClaimType = request.ClaimType!.Trim(),
            EstimatedAmount = request.EstimatedAmount!.Value,
            Status = ClaimStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Evento inicial del historial: no hay estado anterior.
        claim.StatusHistory.Add(new ClaimStatusHistory
        {
            PreviousStatus = null,
            NewStatus = ClaimStatus.Open,
            ChangedAt = now
        });

        dbContext.Claims.Add(claim);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(claim);
    }

    public async Task<ClaimResponse?> UpdateAsync(int id, SaveClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (claim is null)
        {
            return null;
        }

        claim.PolicyNumber = request.PolicyNumber!.Trim();
        claim.InsuredName = request.InsuredName!.Trim();
        claim.ClaimType = request.ClaimType!.Trim();
        claim.EstimatedAmount = request.EstimatedAmount!.Value;
        claim.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(claim);
    }

    public async Task<(ChangeStatusOutcome Outcome, ClaimResponse? Claim)> ChangeStatusAsync(int id, ClaimStatus newStatus, CancellationToken cancellationToken)
    {
        var claim = await dbContext.Claims.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (claim is null)
        {
            return (ChangeStatusOutcome.NotFound, null);
        }

        if (claim.Status == newStatus)
        {
            return (ChangeStatusOutcome.SameStatus, ToResponse(claim));
        }

        var now = DateTime.UtcNow;

        claim.StatusHistory.Add(new ClaimStatusHistory
        {
            PreviousStatus = claim.Status,
            NewStatus = newStatus,
            ChangedAt = now
        });

        claim.Status = newStatus;
        claim.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return (ChangeStatusOutcome.Changed, ToResponse(claim));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await dbContext.Claims
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    public async Task<IReadOnlyList<ClaimStatusHistoryResponse>?> GetStatusHistoryAsync(int id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Claims.AnyAsync(c => c.Id == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await dbContext.ClaimStatusHistories
            .AsNoTracking()
            .Where(h => h.ClaimId == id)
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .Select(h => new ClaimStatusHistoryResponse(h.Id, h.PreviousStatus, h.NewStatus, h.ChangedAt))
            .ToListAsync(cancellationToken);
    }
}
