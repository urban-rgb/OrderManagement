using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.Domain;
using backend.Services.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public class AuthService(
    AppDbContext context,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var exists = await context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                return Result<AuthResponse>.Failure("Email already in use", ErrorType.Conflict);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.User
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return Result<AuthResponse>.Success(GenerateToken(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering user {Email}", request.Email);
            return Result<AuthResponse>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Result<AuthResponse>.Failure("Invalid credentials", ErrorType.Validation);

            return Result<AuthResponse>.Success(GenerateToken(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging in user {Email}", request.Email);
            return Result<AuthResponse>.Failure("Internal error", ErrorType.Failure);
        }
    }

    private AuthResponse GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(
            double.Parse(configuration["Jwt:ExpiresInMinutes"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
