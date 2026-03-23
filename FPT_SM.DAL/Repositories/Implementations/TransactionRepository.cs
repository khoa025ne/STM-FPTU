using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Transaction>> GetByUserAsync(int userId, int page = 1, int pageSize = 10)
        => await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<Transaction?> GetByOrderIdAsync(string orderId)
        => await _context.Transactions.FirstOrDefaultAsync(t => t.OrderId == orderId);

    public async Task<Transaction?> GetByReferenceCodeAsync(string referenceCode)
        => await _context.Transactions.Include(t => t.User).FirstOrDefaultAsync(t => t.ReferenceCode == referenceCode);

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to)
        => await _context.Transactions.Include(t => t.User)
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<decimal> GetTotalRevenueAsync(int? month = null, int? year = null)
    {
        var query = _context.Transactions.Where(t => t.Status == "Success" && t.Type == "TuitionPayment");
        if (month.HasValue) query = query.Where(t => t.CreatedAt.Month == month.Value);
        if (year.HasValue) query = query.Where(t => t.CreatedAt.Year == year.Value);
        return await query.SumAsync(t => t.Amount);
    }

    public async Task<decimal> GetUserBalanceAsync(int userId)
    {
        // Calculate user balance from all successful transactions
        var deposits = await _context.Transactions
            .Where(t => t.UserId == userId && (t.Type == "Deposit" || t.Type == "Refund") && t.Status == "Success")
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var payments = await _context.Transactions
            .Where(t => t.UserId == userId && t.Type == "TuitionPayment" && t.Status == "Success")
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var balance = deposits - payments;
        return Math.Max(0, balance);  // Ensure non-negative
    }
}
