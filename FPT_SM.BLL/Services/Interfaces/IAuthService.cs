using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<UserDto>> LoginAsync(LoginDto dto);
    Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto dto);
    string HashPassword(string password, string email);
    bool VerifyPassword(string password, string email, string hash);
}
