using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter);
    Task<UserDto?> GetByIdAsync(int id);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto);
    Task<ServiceResult> UpdateUserAsync(int id, UpdateUserDto dto);
    Task<ServiceResult> DeleteUserAsync(int id);
    Task<ServiceResult> ToggleActiveAsync(int id);
    Task<ServiceResult> ResetPasswordAsync(int id);
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<ServiceResult<string>> UpdateAvatarAsync(int userId, string fileName, Stream fileStream, string contentType);
    Task<List<UserDto>> GetByRoleAsync(int roleId);
}
