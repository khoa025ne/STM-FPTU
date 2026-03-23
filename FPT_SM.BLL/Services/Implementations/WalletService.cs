using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly IUserRepository _userRepo;
    private readonly ITransactionRepository _transactionRepo;

    public WalletService(IUserRepository userRepo, ITransactionRepository transactionRepo)
    {
        _userRepo = userRepo;
        _transactionRepo = transactionRepo;
    }

    public async Task<WalletDto> GetWalletAsync(int userId)
    {
        var user = await _userRepo.GetWithRoleAsync(userId);
        if (user == null) return new WalletDto { UserId = userId };

        var transactions = await _transactionRepo.GetByUserAsync(userId, 1, 10);

        return new WalletDto
        {
            UserId = userId,
            FullName = user.FullName,
            Balance = user.WalletBalance,
            RecentTransactions = transactions.Select(MapToDto).ToList()
        };
    }

    public async Task<ServiceResult> AddFundsAsync(
        int userId,
        decimal amount,
        string? vnPayTxnId = null,
        string? orderId = null,
        string? referenceCode = null)
    {
        // Validate amount
        const decimal MIN_AMOUNT = 10000m;
        const decimal MAX_AMOUNT = 100000000m;

        if (amount <= 0)
            return ServiceResult.Failure("Số tiền nạp không hợp lệ.");
        if (amount < MIN_AMOUNT)
            return ServiceResult.Failure($"Số tiền nạp tối thiểu là {MIN_AMOUNT:N0} VNĐ.");
        if (amount > MAX_AMOUNT)
            return ServiceResult.Failure($"Số tiền nạp tối đa là {MAX_AMOUNT:N0} VNĐ.");

        // Verify user exists
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ServiceResult.Failure("Không tìm thấy người dùng.");

        // Update wallet balance
        user.WalletBalance += amount;
        await _userRepo.UpdateAsync(user);

        // Create transaction record
        var transaction = new Transaction
        {
            UserId = userId,
            Amount = amount,
            Type = "Deposit",
            Status = "Success",
            Description = $"Nạp tiền vào ví: {amount:N0} VND",
            VnPayTransactionId = vnPayTxnId,
            OrderId = orderId,
            ReferenceCode = referenceCode,
            CreatedAt = DateTime.Now
        };
        await _transactionRepo.AddAsync(transaction);

        return ServiceResult.Success($"Nạp {amount:N0} VND thành công! Số dư mới: {user.WalletBalance:N0} VND");
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 10)
    {
        var transactions = await _transactionRepo.GetByUserAsync(userId, page, pageSize);
        return transactions.Select(MapToDto).ToList();
    }

    public async Task<int> GetTransactionCountAsync(int userId)
    {
        return await _transactionRepo.CountAsync(t => t.UserId == userId);
    }

    private TransactionDto MapToDto(Transaction t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        UserName = t.User?.FullName ?? "",
        Amount = t.Amount,
        Type = t.Type,
        Status = t.Status,
        Description = t.Description,
        VnPayTransactionId = t.VnPayTransactionId,
        OrderId = t.OrderId,
        CreatedAt = t.CreatedAt
    };
}
