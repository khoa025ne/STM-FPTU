using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IAttendanceRepository : IGenericRepository<Attendance>
{
    Task<IEnumerable<Attendance>> GetByEnrollmentAsync(int enrollmentId);
    Task<IEnumerable<Attendance>> GetByClassAndDateAsync(int classId, DateTime date);
    Task<int> CountAbsentAsync(int enrollmentId);
    Task AddRangeAsync(IEnumerable<Attendance> attendances);
}
