using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class GradeRepository : GenericRepository<Grade>, IGradeRepository
{
    public GradeRepository(AppDbContext context) : base(context) { }

    public async Task<Grade?> GetByEnrollmentAsync(int enrollmentId)
        => await _context.Grades.Include(g => g.Enrollment).ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(g => g.EnrollmentId == enrollmentId);

    public async Task<IEnumerable<Grade>> GetByClassAsync(int classId)
        => await _context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e.Student)
            .Include(g => g.Enrollment).ThenInclude(e => e.Class).ThenInclude(c => c.Subject)
            .Include(g => g.Enrollment).ThenInclude(e => e.Semester)
            .Include(g => g.AcademicAnalysis)
            .Where(g => g.Enrollment.ClassId == classId)
            .ToListAsync();

    public async Task<IEnumerable<Grade>> GetByStudentAsync(int studentId)
        => await _context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e.Class).ThenInclude(c => c.Subject)
            .Include(g => g.Enrollment).ThenInclude(e => e.Semester)
            .Where(g => g.Enrollment.StudentId == studentId)
            .ToListAsync();

    public async Task<Grade?> GetStudentGradeForSubjectAsync(int studentId, int subjectId)
        => await _context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e.Class)
            .Where(g => g.Enrollment.StudentId == studentId && g.Enrollment.Class.SubjectId == subjectId && g.Status == 2)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<Grade>> GetPublishedByStudentAsync(int studentId)
        => await _context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e.Class).ThenInclude(c => c.Subject)
            .Include(g => g.Enrollment).ThenInclude(e => e.Semester)
            .Where(g => g.Enrollment.StudentId == studentId && g.Status == 2)
            .ToListAsync();
}
