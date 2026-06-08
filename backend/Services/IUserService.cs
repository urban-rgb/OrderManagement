using backend.Domain;
using backend.Services.DTOs;

namespace backend.Services;

public interface IUserService
{
    Task<Result<IEnumerable<UserResponse>>> GetUsersAsync();
    Task<Result<bool>> UpdateUserRoleAsync(Guid id, UserRole newRole);
    Task<Result<bool>> DeleteUserAsync(Guid id);
}
