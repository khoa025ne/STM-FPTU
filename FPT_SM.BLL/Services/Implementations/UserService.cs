using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuthService _authService;
    private readonly string _avatarPath;

    public UserService(IUserRepository userRepo, IAuthService authService)
    {
        _userRepo = userRepo;
        _authService = authService;
        _avatarPath = Path.Combine("wwwroot", "uploads", "avatars");
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter)
    {
        var all = await _userRepo.GetAllWithRolesAsync();
        var query = all.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
            query = query.Where(u => u.FullName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)
                || u.Email.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)
                || (u.RollNumber != null && u.RollNumber.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)));

        if (filter.RoleId.HasValue)
            query = query.Where(u => u.RoleId == filter.RoleId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        var total = query.Count();
        var items = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).Select(MapToDto).ToList();

        return new PagedResult<UserDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var u = await _userRepo.GetWithRoleAsync(id);
        return u == null ? null : MapToDto(u);
    }

    public async Task<List<UserDto>> GetByRoleAsync(int roleId)
    {
        var users = await _userRepo.GetByRoleAsync(roleId);
        return users.Select(MapToDto).ToList();
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email))
            return ServiceResult<UserDto>.Failure("Email này đã được sử dụng.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = _authService.HashPassword(dto.Password, dto.Email),
            RoleId = dto.RoleId,
            WalletBalance = dto.WalletBalance,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        if (dto.RoleId == 4)
        {
            var rng = new Random();
            string roll;
            do { roll = "SE" + rng.Next(100000, 999999); }
            while (await _userRepo.RollNumberExistsAsync(roll));
            user.RollNumber = roll;
        }

        await _userRepo.AddAsync(user);
        var created = await _userRepo.GetWithRoleAsync(user.Id);
        return ServiceResult<UserDto>.Success(MapToDto(created!), "Tạo tài khoản thành công!");
    }

    public async Task<ServiceResult> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");

        user.FullName = dto.FullName;
        user.AvatarUrl = dto.AvatarUrl;
        user.WalletBalance = dto.WalletBalance;
        user.IsActive = dto.IsActive;
        await _userRepo.UpdateAsync(user);
        return ServiceResult.Success("Cập nhật tài khoản thành công!");
    }

    public async Task<ServiceResult> DeleteUserAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        user.IsActive = false;
        await _userRepo.UpdateAsync(user);
        return ServiceResult.Success("Đã vô hiệu hóa tài khoản.");
    }

    public async Task<ServiceResult> ToggleActiveAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        user.IsActive = !user.IsActive;
        await _userRepo.UpdateAsync(user);
        return ServiceResult.Success(user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
    }

    public async Task<ServiceResult> ResetPasswordAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        user.PasswordHash = _authService.HashPassword("FPT@123456", user.Email);
        await _userRepo.UpdateAsync(user);
        return ServiceResult.Success("Đã đặt lại mật khẩu về: FPT@123456");
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        if (!_authService.VerifyPassword(dto.OldPassword, user.Email, user.PasswordHash))
            return ServiceResult.Failure("Mật khẩu hiện tại không đúng.");
        if (dto.NewPassword != dto.ConfirmPassword)
            return ServiceResult.Failure("Mật khẩu xác nhận không khớp.");
        user.PasswordHash = _authService.HashPassword(dto.NewPassword, user.Email);
        await _userRepo.UpdateAsync(user);
        return ServiceResult.Success("Đổi mật khẩu thành công!");
    }

    public async Task<ServiceResult<string>> UpdateAvatarAsync(int userId, string fileName, Stream fileStream, string contentType)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return ServiceResult<string>.Failure("Không tìm thấy tài khoản.");

        var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        var normalizedContentType = contentType?.ToLowerInvariant() ?? "";
        if (!allowed.Contains(normalizedContentType))
            return ServiceResult<string>.Failure("Chỉ chấp nhận file JPG, PNG, GIF, WEBP.");

        if (fileStream.Length > 5 * 1024 * 1024)
            return ServiceResult<string>.Failure("File không được vượt quá 5MB.");

        var ext = Path.GetExtension(fileName);
        var newFileName = $"avatar_{userId}_{DateTime.Now.Ticks}{ext}";
        var fullPath = Path.Combine(_avatarPath, newFileName);

        Directory.CreateDirectory(_avatarPath);
        using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs);

        user.AvatarUrl = $"/uploads/avatars/{newFileName}";
        await _userRepo.UpdateAsync(user);
        return ServiceResult<string>.Success(user.AvatarUrl, "Cập nhật ảnh đại diện thành công!");
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        RollNumber = u.RollNumber,
        FullName = u.FullName,
        Email = u.Email,
        RoleId = u.RoleId,
        RoleName = u.Role?.Name ?? "",
        WalletBalance = u.WalletBalance,
        AvatarUrl = u.AvatarUrl,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}
