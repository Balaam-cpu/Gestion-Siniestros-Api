using GestionSiniestros.Api.Models;

namespace GestionSiniestros.Api.Dtos;

public record ClaimResponse(
    int Id,
    string PolicyNumber,
    string InsuredName,
    string ClaimType,
    decimal EstimatedAmount,
    ClaimStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
    );
