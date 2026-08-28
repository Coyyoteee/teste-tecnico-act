using System.ComponentModel.DataAnnotations;
using Challenge.Api.Domain;

namespace Challenge.Api.Contracts.Requests;

public sealed record CreateMovementRequest(
    [Required] MovementType? Type,
    [Required] decimal? Amount);
