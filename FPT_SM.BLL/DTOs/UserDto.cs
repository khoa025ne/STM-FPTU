namespace FPT_SM.BLL.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string? RollNumber { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public decimal WalletBalance { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int RoleId { get; set; }
    public decimal WalletBalance { get; set; }
}

public class UpdateUserDto
{
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public decimal WalletBalance { get; set; }
    public bool IsActive { get; set; }
}

public class UserFilterDto
{
    public string? SearchText { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class LoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RegisterDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
