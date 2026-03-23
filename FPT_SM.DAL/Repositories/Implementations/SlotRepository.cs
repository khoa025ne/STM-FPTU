using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class SlotRepository : GenericRepository<Slot>, ISlotRepository
{
    public SlotRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Slot>> GetAllOrderedAsync()
        => await _context.Slots.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync();
}
