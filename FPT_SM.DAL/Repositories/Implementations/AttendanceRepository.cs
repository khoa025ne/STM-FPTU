using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class AttendanceRepository : GenericRepository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Attendance>> GetByEnrollmentAsync(int enrollmentId)
        => await _context.Attendances.Where(a => a.EnrollmentId == enrollmentId)
            .OrderBy(a => a.SessionNumber).ToListAsync();

    public async Task<IEnumerable<Attendance>> GetByClassAndDateAsync(int classId, DateTime date)
        => await _context.Attendances
            .Include(a => a.Enrollment).ThenInclude(e => e.Student)
            .Where(a => a.Enrollment.ClassId == classId && a.SlotDate.Date == date.Date)
            .ToListAsync();

    public async Task<int> CountAbsentAsync(int enrollmentId)
        => await _context.Attendances
            .CountAsync(a => a.EnrollmentId == enrollmentId && a.Status == "Absent");

    public async Task AddRangeAsync(IEnumerable<Attendance> attendances)
    {
        await _context.Attendances.AddRangeAsync(attendances);
        await _context.SaveChangesAsync();
    }
}
