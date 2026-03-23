using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IWalletService
{
    Task<WalletDto> GetWalletAsync(int userId);
    Task<ServiceResult> AddFundsAsync(
        int userId,
        decimal amount,
        string? vnPayTxnId = null,
        string? orderId = null,
        string? referenceCode = null);
    Task<List<TransactionDto>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 10);
    Task<int> GetTransactionCountAsync(int userId);
}

public interface IVnPayService
{
    /// <summary>
    /// DEPRECATED: Use CreatePaymentUrlAsync instead.
    /// </summary>
    string CreatePaymentUrl(string ipAddress, VnPayRequestDto dto);

    /// <summary>
    /// Create payment URL with full validation and transaction tracking.
    /// </summary>
    Task<ServiceResult<DepositResponseDTO>> CreatePaymentUrlAsync(int userId, decimal amount, string ipAddress);

    /// <summary>
    /// Process VNPay callback from Return URL.
    /// </summary>
    Task<ServiceResult<decimal>> ProcessCallbackAsync(IDictionary<string, string> query);

    /// <summary>
    /// Process VNPay IPN (Instant Payment Notification) from server-to-server callback.
    /// </summary>
    Task<ServiceResult<decimal>> ProcessIpnAsync(IDictionary<string, string> query);
}
