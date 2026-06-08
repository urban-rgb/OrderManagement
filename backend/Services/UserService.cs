using backend.Data;
using backend.Domain;
using backend.Services.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserService(AppDbContext context, ILogger<UserService> logger) : IUserService
{
    public async Task<Result<IEnumerable<UserResponse>>> GetUsersAsync()
    {
        try
        {
            var users = await context.Users
                .AsNoTracking()
                .Select(u => new UserResponse(u.Id, u.Email, u.Role.ToString()))
                .ToListAsync();
            return Result<IEnumerable<UserResponse>>.Success(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching users");
            return Result<IEnumerable<UserResponse>>.Failure("Database error", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> UpdateUserRoleAsync(Guid id, UserRole newRole)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return Result<bool>.Failure("User not found", ErrorType.NotFound);

            user.Role = newRole;
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating role for user {Id}", id);
            return Result<bool>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid id)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return Result<bool>.Failure("User not found", ErrorType.NotFound);

            context.Users.Remove(user);
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user {Id}", id);
            return Result<bool>.Failure("Internal error", ErrorType.Failure);
        }
    }
}
