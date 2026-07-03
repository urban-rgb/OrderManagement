namespace backend.Services.DTOs;

public record AuthResponse(string Token, DateTime ExpiresAt);