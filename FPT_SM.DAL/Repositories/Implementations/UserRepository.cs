using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetWithRoleAsync(int id)
        => await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<IEnumerable<User>> GetByRoleAsync(int roleId)
        => await _context.Users.Include(u => u.Role).Where(u => u.RoleId == roleId).ToListAsync();

    public async Task<IEnumerable<User>> GetAllWithRolesAsync()
        => await _context.Users.Include(u => u.Role).OrderBy(u => u.FullName).ToListAsync();

    public async Task<bool> EmailExistsAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email);

    public async Task<bool> RollNumberExistsAsync(string rollNumber)
        => await _context.Users.AnyAsync(u => u.RollNumber == rollNumber);
}
