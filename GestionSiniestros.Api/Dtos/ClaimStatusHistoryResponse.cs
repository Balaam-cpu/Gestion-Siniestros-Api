using GestionSiniestros.Api.Models;

namespace GestionSiniestros.Api.Dtos;

public record ClaimStatusHistoryResponse(
    int Id,
    ClaimStatus? PreviousStatus,
    ClaimStatus NewStatus,
    DateTime ChangedAt
    );
