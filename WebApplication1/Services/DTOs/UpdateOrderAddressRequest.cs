using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace WebApplication1.Services.DTOs;

[ExcludeFromCodeCoverage]
public record UpdateOrderAddressRequest(
    [Required]
    string NewAddress
);