using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface ISlotRepository : IGenericRepository<Slot>
{
    Task<IEnumerable<Slot>> GetAllOrderedAsync();
}
