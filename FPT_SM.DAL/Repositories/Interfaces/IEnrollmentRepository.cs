using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IEnrollmentRepository : IGenericRepository<Enrollment>
{
    Task<IEnumerable<Enrollment>> GetByStudentAsync(int studentId, int? semesterId = null);
    Task<IEnumerable<Enrollment>> GetByClassAsync(int classId);
    Task<Enrollment?> GetWithDetailsAsync(int id);
    Task<Enrollment?> GetByStudentAndClassAsync(int studentId, int classId);
    Task<bool> IsStudentEnrolledInSubjectAsync(int studentId, int subjectId, int semesterId);
    Task<IEnumerable<Enrollment>> GetStudentEnrollmentsWithGradesAsync(int studentId);
}
