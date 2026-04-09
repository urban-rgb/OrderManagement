using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Services.DTOs;

public record UpdateOrderAddressRequest(
    [Required]
    string NewAddress
);