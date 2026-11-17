using GestionSiniestros.Api.Dtos;
using GestionSiniestros.Api.Models;

namespace GestionSiniestros.Api.Interfaces;

public interface IClaimService
{
    Task<IReadOnlyList<ClaimResponse>> GetAllAsync(string? policyNumber, ClaimStatus? status, CancellationToken cancellationToken);
    Task<ClaimResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ClaimResponse> CreateAsync(SaveClaimRequest request, CancellationToken cancellationToken);
    Task<ClaimResponse?> UpdateAsync(int id, SaveClaimRequest request, CancellationToken cancellationToken);
    Task<(ChangeStatusOutcome Outcome, ClaimResponse? Claim)> ChangeStatusAsync(int id, ClaimStatus newStatus, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClaimStatusHistoryResponse>?> GetStatusHistoryAsync(int id, CancellationToken cancellationToken);
}

public enum ChangeStatusOutcome
{
    Changed,
    NotFound,
    SameStatus
}
