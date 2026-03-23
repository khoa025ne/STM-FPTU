using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Tạo URL thanh toán VNPay với xác thực đầy đủ
    /// </summary>
    Task<ServiceResult<DepositResponseDTO>> CreatePaymentUrlAsync(
        int userId,
        decimal amount,
        string ipAddress);

    /// <summary>
    /// Lấy danh sách giao dịch của người dùng (có phân trang)
    /// </summary>
    Task<ServiceResult<List<TransactionDto>>> GetUserTransactionsAsync(
        int userId,
        int page = 1,
        int pageSize = 10,
        string? transactionType = null);

    /// <summary>
    /// Đếm tổng số giao dịch của người dùng
    /// </summary>
    Task<ServiceResult<int>> GetUserTransactionCountAsync(int userId);

    /// <summary>
    /// Xác thực số tiền nạp (min/max)
    /// </summary>
    ServiceResult ValidateDepositAmount(decimal amount);
}
