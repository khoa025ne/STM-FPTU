using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
{
    public SubjectRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Subject>> GetAllWithPrerequisitesAsync()
        => await _context.Subjects
            .Include(s => s.Prerequisites).ThenInclude(p => p.PrerequisiteSubject)
            .Include(s => s.Classes).ThenInclude(c => c.Semester)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .ToListAsync();

    public async Task<Subject?> GetWithPrerequisitesAsync(int id)
        => await _context.Subjects
            .Include(s => s.Prerequisites).ThenInclude(p => p.PrerequisiteSubject)
            .Include(s => s.Classes).ThenInclude(c => c.Semester)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Prerequisite>> GetPrerequisitesAsync(int subjectId)
        => await _context.Prerequisites
            .Include(p => p.PrerequisiteSubject)
            .Where(p => p.SubjectId == subjectId)
            .ToListAsync();
}
