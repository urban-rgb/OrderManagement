using System.ComponentModel.DataAnnotations;

namespace backend.Services.DTOs;

public record UpdateOrderAddressRequest(
    [Required]
    string NewAddress
);