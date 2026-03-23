using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface ISubjectRepository : IGenericRepository<Subject>
{
    Task<IEnumerable<Subject>> GetAllWithPrerequisitesAsync();
    Task<Subject?> GetWithPrerequisitesAsync(int id);
    Task<IEnumerable<Prerequisite>> GetPrerequisitesAsync(int subjectId);
}
