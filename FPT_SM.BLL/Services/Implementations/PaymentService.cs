using System.Net;
using System.Security.Cryptography;
using System.Text;
using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FPT_SM.BLL.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    // Deposit amount limits (VND)
    private const decimal MIN_DEPOSIT = 10000m;      // 10k VND
    private const decimal MAX_DEPOSIT = 100000000m;  // 100M VND

    public PaymentService(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<DepositResponseDTO>> CreatePaymentUrlAsync(
        int userId,
        decimal amount,
        string ipAddress)
    {
        // Validate amount
        var amountValidation = ValidateDepositAmount(amount);
        if (!amountValidation.IsSuccess)
            return ServiceResult<DepositResponseDTO>.Failure(amountValidation.Message ?? "Số tiền không hợp lệ");

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return ServiceResult<DepositResponseDTO>.Failure("Không tìm thấy người dùng.");

        // Vietnam timezone (UTC+7) - MUST use for VNPay
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // Generate unique reference code and order ID
        var referenceCode = $"DEP{vnTime:yyyyMMddHHmmss}{userId:D4}{new Random().Next(1000, 9999)}";
        var orderId = $"{userId}_{DateTime.UtcNow.Ticks}";

        // Create Pending transaction record
        var transaction = new Transaction
        {
            UserId = userId,
            Amount = amount,
            Type = "Deposit",
            Status = "Pending",
            ReferenceCode = referenceCode,
            OrderId = orderId,
            Description = $"Nạp tiền ví: {amount:N0} VND",
            CreatedAt = vnTime
        };

        await _transactionRepository.AddAsync(transaction);

        // Build VNPay payment URL
        var paymentUrl = BuildVnPayUrl(referenceCode, amount, ipAddress, $"Nap tien tai khoan {user.RollNumber ?? user.Email}");

        var expiresAt = vnTime.AddMinutes(15);

        return ServiceResult<DepositResponseDTO>.Success(
            new DepositResponseDTO
            {
                TransactionId = transaction.Id,
                ReferenceCode = referenceCode,
                OrderId = orderId,
                Amount = amount,
                Currency = "VND",
                PaymentUrl = paymentUrl,
                CreatedAt = vnTime,
                ExpiresAt = expiresAt
            },
            "Tạo liên kết thanh toán thành công.");
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<TransactionDto>>> GetUserTransactionsAsync(
        int userId,
        int page = 1,
        int pageSize = 10,
        string? transactionType = null)
    {
        var allTransactions = await _transactionRepository.GetByUserAsync(userId, page, pageSize);

        var transactions = string.IsNullOrEmpty(transactionType)
            ? allTransactions.ToList()
            : allTransactions.Where(t => t.Type == transactionType).ToList();

        return ServiceResult<List<TransactionDto>>.Success(
            transactions.Select(MapToDto).ToList(),
            "Lấy danh sách giao dịch thành công.");
    }

    /// <inheritdoc />
    public async Task<ServiceResult<int>> GetUserTransactionCountAsync(int userId)
    {
        var transactions = await _transactionRepository.GetByUserAsync(userId, 1, int.MaxValue);
        return ServiceResult<int>.Success(
            transactions.Count(),
            "Đếm giao dịch thành công.");
    }

    /// <inheritdoc />
    public ServiceResult ValidateDepositAmount(decimal amount)
    {
        if (amount < MIN_DEPOSIT)
            return ServiceResult.Failure($"Số tiền nạp tối thiểu là {MIN_DEPOSIT:N0} VNĐ.");

        if (amount > MAX_DEPOSIT)
            return ServiceResult.Failure($"Số tiền nạp tối đa là {MAX_DEPOSIT:N0} VNĐ.");

        return ServiceResult.Success();
    }

    // ===== PRIVATE: Build VNPay URL =====

    private string BuildVnPayUrl(string txnRef, decimal amount, string ipAddress, string orderInfo)
    {
        var vnp = _configuration.GetSection("VnPay");
        var tmnCode = vnp["TmnCode"] ?? "";
        var hashSecret = vnp["HashSecret"] ?? "";
        var baseUrl = vnp["Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var returnUrl = vnp["ReturnUrl"] ?? "";
        var version = vnp["Version"] ?? "2.1.0";
        var command = vnp["Command"] ?? "pay";
        var currCode = vnp["CurrCode"] ?? "VND";
        var locale = vnp["Locale"] ?? "vn";

        // VNPay amount = amount * 100 (no decimal)
        var vnpAmount = ((long)(amount * 100)).ToString();

        // Vietnam timezone for VNPay dates (MUST use "SE Asia Standard Time")
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        var createDate = vnTime.ToString("yyyyMMddHHmmss");
        var expireDate = vnTime.AddMinutes(15).ToString("yyyyMMddHHmmss");

        // Build sorted query parameters
        var vnpParams = new SortedDictionary<string, string>
        {
            { "vnp_Version", version },
            { "vnp_Command", command },
            { "vnp_TmnCode", tmnCode },
            { "vnp_Amount", vnpAmount },
            { "vnp_CurrCode", currCode },
            { "vnp_TxnRef", txnRef },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", locale },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_IpAddr", ipAddress },
            { "vnp_CreateDate", createDate },
            { "vnp_ExpireDate", expireDate }
        };

        // Build query string (without hash)
        var queryBuilder = new StringBuilder();
        foreach (var kv in vnpParams)
        {
            if (queryBuilder.Length > 0) queryBuilder.Append('&');
            queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
            queryBuilder.Append('=');
            queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
        }

        var signData = queryBuilder.ToString();

        // HMAC-SHA512 signature
        var hash = HmacSha512(hashSecret, signData);

        return $"{baseUrl}?{signData}&vnp_SecureHash={hash}";
    }

    internal static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private static TransactionDto MapToDto(Transaction t) => new()
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
