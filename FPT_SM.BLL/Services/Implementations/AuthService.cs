using System.Security.Cryptography;
using System.Text;
using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;

    public AuthService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ServiceResult<UserDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email);
        if (user == null)
            return ServiceResult<UserDto>.Failure("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            return ServiceResult<UserDto>.Failure("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

        if (!VerifyPassword(dto.Password, dto.Email, user.PasswordHash))
            return ServiceResult<UserDto>.Failure("Email hoặc mật khẩu không đúng.");

        var userDto = MapToDto(user);
        return ServiceResult<UserDto>.Success(userDto, "Đăng nhập thành công!");
    }

    public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return ServiceResult<UserDto>.Failure("Mật khẩu xác nhận không khớp.");

        if (await _userRepo.EmailExistsAsync(dto.Email))
            return ServiceResult<UserDto>.Failure("Email này đã được sử dụng.");

        var rollNumber = await GenerateUniqueRollNumberAsync();
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password, dto.Email),
            RoleId = 4,
            RollNumber = rollNumber,
            IsActive = true,
            WalletBalance = 0,
            CreatedAt = DateTime.Now
        };

        await _userRepo.AddAsync(user);
        var created = await _userRepo.GetByEmailAsync(dto.Email);
        return ServiceResult<UserDto>.Success(MapToDto(created!), "Đăng ký thành công! Mã số sinh viên: " + rollNumber);
    }

    public string HashPassword(string password, string email)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(email.ToLower()));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string email, string hash)
    {
        var computed = HashPassword(password, email);
        return computed == hash;
    }

    private async Task<string> GenerateUniqueRollNumberAsync()
    {
        var rng = new Random();
        string rollNumber;
        do
        {
            rollNumber = "SE" + rng.Next(100000, 999999).ToString();
        } while (await _userRepo.RollNumberExistsAsync(rollNumber));
        return rollNumber;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        RollNumber = user.RollNumber,
        FullName = user.FullName,
        Email = user.Email,
        RoleId = user.RoleId,
        RoleName = user.Role?.Name ?? "",
        WalletBalance = user.WalletBalance,
        AvatarUrl = user.AvatarUrl,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
