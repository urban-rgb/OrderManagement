using System.ComponentModel.DataAnnotations;

namespace backend.Services.DTOs;

public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password
);
