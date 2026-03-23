using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithRoleAsync(int id);
    Task<IEnumerable<User>> GetByRoleAsync(int roleId);
    Task<IEnumerable<User>> GetAllWithRolesAsync();
    Task<bool> EmailExistsAsync(string email);
    Task<bool> RollNumberExistsAsync(string rollNumber);
}
