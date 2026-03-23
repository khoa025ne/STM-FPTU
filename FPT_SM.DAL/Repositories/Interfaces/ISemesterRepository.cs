using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface ISemesterRepository : IGenericRepository<Semester>
{
    Task<Semester?> GetOngoingSemesterAsync();
    Task<IEnumerable<Semester>> GetOrderedAsync();
}
