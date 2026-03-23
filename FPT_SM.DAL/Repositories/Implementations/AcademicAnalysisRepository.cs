using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class AcademicAnalysisRepository : GenericRepository<AcademicAnalysis>, IAcademicAnalysisRepository
{
    public AcademicAnalysisRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<AcademicAnalysis>> GetByStudentAsync(int studentId)
        => await _context.AcademicAnalyses
            .Include(a => a.Class).ThenInclude(c => c.Subject)
            .Include(a => a.Semester)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<AcademicAnalysis?> GetLatestForStudentClassAsync(int studentId, int classId)
        => await _context.AcademicAnalyses
            .Where(a => a.StudentId == studentId && a.ClassId == classId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<GradeAnalysisTemplate>> GetActiveTemplatesAsync()
        => await _context.GradeAnalysisTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.MinGPA)
            .ToListAsync();
}
