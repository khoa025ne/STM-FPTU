using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IClassRepository : IGenericRepository<Class>
{
    Task<Class?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Class>> GetAllWithDetailsAsync();
    Task<IEnumerable<Class>> GetBySemesterAsync(int semesterId);
    Task<IEnumerable<Class>> GetByTeacherAsync(int teacherId);
    Task<IEnumerable<Class>> GetByTeacherAndSemesterAsync(int teacherId, int semesterId);
    Task<IEnumerable<Class>> GetBySubjectAndSemesterAsync(int subjectId, int semesterId);
}
