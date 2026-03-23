using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Enrollment>> GetByStudentAsync(int studentId, int? semesterId = null)
    {
        var query = _context.Enrollments
            .Include(e => e.Class).ThenInclude(c => c.Subject)
            .Include(e => e.Class).ThenInclude(c => c.Slot)
            .Include(e => e.Class).ThenInclude(c => c.Teacher)
            .Include(e => e.Semester)
            .Include(e => e.Grade)
            .Where(e => e.StudentId == studentId);
        if (semesterId.HasValue) query = query.Where(e => e.SemesterId == semesterId.Value);
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Enrollment>> GetByClassAsync(int classId)
        => await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Grade)
            .Where(e => e.ClassId == classId)
            .ToListAsync();

    public async Task<Enrollment?> GetWithDetailsAsync(int id)
        => await _context.Enrollments
            .Include(e => e.Class).ThenInclude(c => c.Subject)
            .Include(e => e.Student)
            .Include(e => e.Grade)
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Enrollment?> GetByStudentAndClassAsync(int studentId, int classId)
        => await _context.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId && e.ClassId == classId);

    public async Task<bool> IsStudentEnrolledInSubjectAsync(int studentId, int subjectId, int semesterId)
        => await _context.Enrollments
            .Include(e => e.Class)
            .AnyAsync(e => e.StudentId == studentId && e.Class.SubjectId == subjectId && e.SemesterId == semesterId && e.Status != 4);

    public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsWithGradesAsync(int studentId)
        => await _context.Enrollments
            .Include(e => e.Class).ThenInclude(c => c.Subject)
            .Include(e => e.Semester)
            .Include(e => e.Grade)
            .Where(e => e.StudentId == studentId && e.Status != 4)
            .ToListAsync();
}
