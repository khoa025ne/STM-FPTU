using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IGradeRepository : IGenericRepository<Grade>
{
    Task<Grade?> GetByEnrollmentAsync(int enrollmentId);
    Task<IEnumerable<Grade>> GetByClassAsync(int classId);
    Task<IEnumerable<Grade>> GetByStudentAsync(int studentId);
    Task<Grade?> GetStudentGradeForSubjectAsync(int studentId, int subjectId);
    Task<IEnumerable<Grade>> GetPublishedByStudentAsync(int studentId);
}
