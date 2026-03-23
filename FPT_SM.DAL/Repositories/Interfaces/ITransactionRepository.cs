using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface ITransactionRepository : IGenericRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByUserAsync(int userId, int page = 1, int pageSize = 10);
    Task<Transaction?> GetByOrderIdAsync(string orderId);
    Task<Transaction?> GetByReferenceCodeAsync(string referenceCode);
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<decimal> GetTotalRevenueAsync(int? month = null, int? year = null);
}
