using System.ComponentModel.DataAnnotations;
using GestionSiniestros.Api.Models;

namespace GestionSiniestros.Api.Dtos;

public class ChangeClaimStatusRequest
{
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public ClaimStatus? Status { get; set; }
}
