using System.ComponentModel.DataAnnotations;

namespace GestionSiniestros.Api.Dtos;

public class SaveClaimRequest : IValidatableObject
{
    [Required(ErrorMessage = "El número de póliza es obligatorio.")]
    [StringLength(50, ErrorMessage = "El número de póliza no puede exceder 50 caracteres.")]
    public string? PolicyNumber { get; set; }

    [Required(ErrorMessage = "El nombre del asegurado es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre del asegurado no puede exceder 200 caracteres.")]
    public string? InsuredName { get; set; }

    [Required(ErrorMessage = "El tipo de siniestro es obligatorio.")]
    [StringLength(100, ErrorMessage = "El tipo de siniestro no puede exceder 100 caracteres.")]
    public string? ClaimType { get; set; }

    [Required(ErrorMessage = "El monto estimado es obligatorio.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "El monto estimado debe ser mayor que cero.")]
    public decimal? EstimatedAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EstimatedAmount is decimal amount && decimal.Round(amount, 2) != amount)
        {
            yield return new ValidationResult("El monto estimado admite como máximo dos decimales.", [nameof(EstimatedAmount)]);
        }
    }
}
