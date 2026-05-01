using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Services.DTOs;

[ExcludeFromCodeCoverage]
public record UpdateOrderAddressRequest(
    [Required]
    string NewAddress
);