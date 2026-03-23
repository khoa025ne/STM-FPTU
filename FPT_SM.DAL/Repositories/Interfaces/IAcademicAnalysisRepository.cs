using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IAcademicAnalysisRepository : IGenericRepository<AcademicAnalysis>
{
    Task<IEnumerable<AcademicAnalysis>> GetByStudentAsync(int studentId);
    Task<AcademicAnalysis?> GetLatestForStudentClassAsync(int studentId, int classId);
    Task<IEnumerable<GradeAnalysisTemplate>> GetActiveTemplatesAsync();
}
