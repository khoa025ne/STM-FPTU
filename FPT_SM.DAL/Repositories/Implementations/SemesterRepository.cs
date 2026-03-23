using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class SemesterRepository : GenericRepository<Semester>, ISemesterRepository
{
    public SemesterRepository(AppDbContext context) : base(context) { }

    public async Task<Semester?> GetOngoingSemesterAsync()
        => await _context.Semesters
            .Where(s => s.DeletedAt == null)
            .FirstOrDefaultAsync(s => s.Status == 2);

    public async Task<IEnumerable<Semester>> GetOrderedAsync()
        => await _context.Semesters
            .Where(s => s.DeletedAt == null)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
}
