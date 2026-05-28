using System.ComponentModel.DataAnnotations;

namespace backend.Services.DTOs;

public record RegisterRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    [MinLength(8)]
    string Password
);
