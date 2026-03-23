using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class ClassRepository : GenericRepository<Class>, IClassRepository
{
    public ClassRepository(AppDbContext context) : base(context) { }

    private IQueryable<Class> WithDetails()
        => _context.Classes
            .Include(c => c.Subject)
            .Include(c => c.Teacher)
            .Include(c => c.Semester)
            .Include(c => c.Slot);

    public async Task<Class?> GetWithDetailsAsync(int id)
        => await WithDetails().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Class>> GetAllWithDetailsAsync()
        => await WithDetails().OrderByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<IEnumerable<Class>> GetBySemesterAsync(int semesterId)
        => await WithDetails().Where(c => c.SemesterId == semesterId).ToListAsync();

    public async Task<IEnumerable<Class>> GetByTeacherAsync(int teacherId)
        => await WithDetails().Where(c => c.TeacherId == teacherId).ToListAsync();

    public async Task<IEnumerable<Class>> GetByTeacherAndSemesterAsync(int teacherId, int semesterId)
        => await WithDetails().Where(c => c.TeacherId == teacherId && c.SemesterId == semesterId).ToListAsync();

    public async Task<IEnumerable<Class>> GetBySubjectAndSemesterAsync(int subjectId, int semesterId)
        => await WithDetails().Where(c => c.SubjectId == subjectId && c.SemesterId == semesterId).ToListAsync();
}
