using System.Globalization;
using System.Net;
using System.Text;
using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FPT_SM.BLL.Services.Implementations;

public class PaymentCallbackService : IPaymentCallbackService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentCallbackService> _logger;

    public PaymentCallbackService(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<PaymentCallbackService> logger)
    {
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<PaymentResultDTO>> ProcessCallbackAsync(
        IDictionary<string, string> queryParams)
    {
        try
        {
            _logger.LogInformation("[Payment] 🔄 Xử lý VNPay callback...");

            // Extract VNPay parameters
            queryParams.TryGetValue("vnp_TxnRef", out var vnpTxnRef);
            queryParams.TryGetValue("vnp_ResponseCode", out var vnpResponseCode);
            queryParams.TryGetValue("vnp_TransactionNo", out var vnpTransactionNo);
            queryParams.TryGetValue("vnp_Amount", out var vnpAmount);
            queryParams.TryGetValue("vnp_PayDate", out var vnpPayDate);
            queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash);

            vnpTxnRef ??= "";
            vnpResponseCode ??= "";
            vnpTransactionNo ??= "";
            vnpAmount ??= "";
            vnpPayDate ??= "";
            vnpSecureHash ??= "";

            _logger.LogInformation($"[Payment] TxnRef: {vnpTxnRef}, ResponseCode: {vnpResponseCode}, Amount: {vnpAmount}");

            var result = new PaymentResultDTO
            {
                ReferenceCode = vnpTxnRef,
                VnPayTransactionNo = vnpTransactionNo,
                Status = vnpResponseCode
            };

            // Parse amount (VNPay sends amount * 100)
            if (long.TryParse(vnpAmount, out var amountLong))
                result.Amount = amountLong / 100m;

            // Parse pay date
            if (DateTime.TryParseExact(vnpPayDate, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var payDate))
            {
                result.ProcessedAt = payDate;
            }
            else
            {
                result.ProcessedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }

            // Verify HMAC-SHA512 signature
            var verifyResult = VerifySignature(queryParams, vnpSecureHash);
            if (!verifyResult.IsSuccess)
            {
                _logger.LogError($"[Payment] ❌ Chữ ký không hợp lệ!");
                return ServiceResult<PaymentResultDTO>.Failure(verifyResult.Message ?? "Chữ ký không hợp lệ");
            }

            _logger.LogInformation($"[Payment] ✅ Chữ ký hợp lệ");

            // Find transaction by reference code (vnp_TxnRef = ReferenceCode)
            var transaction = await _transactionRepository.GetByReferenceCodeAsync(vnpTxnRef);
            if (transaction == null)
            {
                _logger.LogError($"[Payment] ❌ Không tìm thấy giao dịch với ReferenceCode: {vnpTxnRef}");
                return ServiceResult<PaymentResultDTO>.Failure("Không tìm thấy giao dịch.");
            }

            result.TransactionId = transaction.Id;
            result.UserId = transaction.UserId;
            result.OrderId = transaction.OrderId ?? "";

            _logger.LogInformation($"[Payment] ℹ️ Tìm thấy Transaction ID: {transaction.Id}, User ID: {transaction.UserId}");

            // Check if already processed
            if (transaction.Status != "Pending")
            {
                if (transaction.Status == "Success")
                {
                    _logger.LogWarning($"[Payment] ⚠️ Giao dịch đã được xử lý thành công: {vnpTxnRef}");
                    return ServiceResult<PaymentResultDTO>.Success(result, "Giao dịch đã được xử lý.");
                }

                _logger.LogError($"[Payment] ❌ Giao dịch đã thất bại trước đó: {vnpTxnRef}");
                return ServiceResult<PaymentResultDTO>.Failure("Giao dịch đã thất bại trước đó.");
            }

            // Process based on response code
            if (vnpResponseCode == "00")
            {
                _logger.LogInformation($"[Payment] 💰 Xử lý thanh toán thành công cho User {transaction.UserId}, Số tiền: {transaction.Amount:N0} VND");

                // Update transaction status
                transaction.Status = "Success";
                transaction.VnPayTransactionId = vnpTransactionNo;
                await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation($"[Payment] ✅ Cập nhật Transaction thành công");

                // Update wallet balance
                var user = await _userRepository.GetByIdAsync(transaction.UserId);
                if (user != null)
                {
                    var oldBalance = user.WalletBalance;
                    user.WalletBalance += transaction.Amount;
                    await _userRepository.UpdateAsync(user);
                    _logger.LogInformation($"[Payment] ✅ Cập nhật Wallet: {oldBalance:N0} VND → {user.WalletBalance:N0} VND");
                    result.WalletBalance = user.WalletBalance;
                }
                else
                {
                    _logger.LogError($"[Payment] ❌ Không tìm thấy User {transaction.UserId}");
                }

                result.IsSuccess = true;
                result.Message = $"Nạp tiền thành công! Số tiền: {transaction.Amount:N0} VNĐ";
                _logger.LogInformation($"[Payment] ✅ Callback thành công: {result.Message}");
                return ServiceResult<PaymentResultDTO>.Success(result);
            }
            else
            {
                // Payment failed
                transaction.Status = "Failed";
                await _transactionRepository.UpdateAsync(transaction);
                _logger.LogWarning($"[Payment] ⚠️ Thanh toán thất bại với mã lỗi: {vnpResponseCode}");

                result.IsSuccess = false;
                result.Message = GetVnPayResponseMessage(vnpResponseCode);
                return ServiceResult<PaymentResultDTO>.Failure(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Payment] ❌ Lỗi xử lý callback: {ex.Message}\n{ex.StackTrace}");
            return ServiceResult<PaymentResultDTO>.Failure($"Lỗi xử lý giao dịch: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<PaymentResultDTO>> ProcessIpnAsync(
        IDictionary<string, string> queryParams)
    {
        try
        {
            _logger.LogInformation("[Payment IPN] 🔄 Xử lý VNPay IPN...");

            queryParams.TryGetValue("vnp_TxnRef", out var vnpTxnRef);
            queryParams.TryGetValue("vnp_ResponseCode", out var vnpResponseCode);
            queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash);

            vnpTxnRef ??= "";
            vnpResponseCode ??= "";
            vnpSecureHash ??= "";

            _logger.LogInformation($"[Payment IPN] TxnRef: {vnpTxnRef}, ResponseCode: {vnpResponseCode}");

            // Verify signature
            var verifyResult = VerifySignature(queryParams, vnpSecureHash);
            if (!verifyResult.IsSuccess)
            {
                _logger.LogError("[Payment IPN] ❌ Chữ ký không hợp lệ");
                return ServiceResult<PaymentResultDTO>.Failure("Chữ ký không hợp lệ");
            }

            _logger.LogInformation("[Payment IPN] ✅ Chữ ký hợp lệ");

            // Find transaction
            var transaction = await _transactionRepository.GetByReferenceCodeAsync(vnpTxnRef);
            if (transaction == null)
            {
                _logger.LogError($"[Payment IPN] ❌ Không tìm thấy giao dịch: {vnpTxnRef}");
                return ServiceResult<PaymentResultDTO>.Failure("Không tìm thấy giao dịch");
            }

            _logger.LogInformation($"[Payment IPN] ℹ️ Tìm thấy Transaction ID: {transaction.Id}");

            // Already processed - idempotent
            if (transaction.Status != "Pending")
            {
                _logger.LogWarning($"[Payment IPN] ⚠️ Giao dịch đã được xử lý. Status: {transaction.Status}");
                return ServiceResult<PaymentResultDTO>.Success(
                    new PaymentResultDTO
                    {
                        TransactionId = transaction.Id,
                        UserId = transaction.UserId,
                        ReferenceCode = transaction.ReferenceCode ?? "",
                        OrderId = transaction.OrderId ?? "",
                        IsSuccess = transaction.Status == "Success",
                        Message = transaction.Status == "Success" ? "Đã xử lý" : "Thất bại",
                        Status = transaction.Status
                    },
                    "Đã xử lý");
            }

            if (vnpResponseCode == "00")
            {
                _logger.LogInformation($"[Payment IPN] 💰 Xử lý thành công cho User {transaction.UserId}");

                transaction.Status = "Success";
                await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation($"[Payment IPN] ✅ Cập nhật Transaction thành công");

                var user = await _userRepository.GetByIdAsync(transaction.UserId);
                if (user != null)
                {
                    var oldBalance = user.WalletBalance;
                    user.WalletBalance += transaction.Amount;
                    await _userRepository.UpdateAsync(user);
                    _logger.LogInformation($"[Payment IPN] ✅ Cập nhật Wallet: {oldBalance:N0} VND → {user.WalletBalance:N0} VND");
                }
                else
                {
                    _logger.LogError($"[Payment IPN] ❌ Không tìm thấy User {transaction.UserId}");
                }

                return ServiceResult<PaymentResultDTO>.Success(
                    new PaymentResultDTO
                    {
                        TransactionId = transaction.Id,
                        UserId = transaction.UserId,
                        ReferenceCode = transaction.ReferenceCode ?? "",
                        OrderId = transaction.OrderId ?? "",
                        IsSuccess = true,
                        Message = "Xác nhận thanh toán thành công",
                        Status = "Success",
                        Amount = transaction.Amount,
                        WalletBalance = user?.WalletBalance ?? 0
                    });
            }
            else
            {
                _logger.LogWarning($"[Payment IPN] ⚠️ Thanh toán thất bại với mã lỗi: {vnpResponseCode}");

                transaction.Status = "Failed";
                await _transactionRepository.UpdateAsync(transaction);

                return ServiceResult<PaymentResultDTO>.Success(
                    new PaymentResultDTO
                    {
                        TransactionId = transaction.Id,
                        UserId = transaction.UserId,
                        ReferenceCode = transaction.ReferenceCode ?? "",
                        OrderId = transaction.OrderId ?? "",
                        IsSuccess = false,
                        Message = "Thanh toán thất bại",
                        Status = "Failed"
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Payment IPN] ❌ Lỗi xử lý IPN: {ex.Message}\n{ex.StackTrace}");
            return ServiceResult<PaymentResultDTO>.Failure($"Lỗi xử lý IPN: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public ServiceResult VerifySignature(
        IDictionary<string, string> parameters,
        string receivedHash)
    {
        try
        {
            var hashSecret = _configuration["VnPay:HashSecret"] ?? "";

            // Build sorted params (exclude vnp_SecureHash and vnp_SecureHashType)
            var vnpParams = new SortedDictionary<string, string>();
            foreach (var kv in parameters)
            {
                if (!string.IsNullOrEmpty(kv.Key)
                    && kv.Key.StartsWith("vnp_")
                    && kv.Key != "vnp_SecureHash"
                    && kv.Key != "vnp_SecureHashType")
                {
                    vnpParams[kv.Key] = kv.Value;
                }
            }

            // Build sign data
            var queryBuilder = new StringBuilder();
            foreach (var kv in vnpParams)
            {
                if (queryBuilder.Length > 0) queryBuilder.Append('&');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
                queryBuilder.Append('=');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
            }

            var signData = queryBuilder.ToString();
            var computedHash = PaymentService.HmacSha512(hashSecret, signData);

            if (!string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Failure("Chữ ký không hợp lệ");

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"Lỗi xác thực chữ ký: {ex.Message}");
        }
    }

    // ===== PRIVATE HELPERS =====

    private static string GetVnPayResponseMessage(string responseCode)
    {
        return responseCode switch
        {
            "00" => "Giao dịch thành công.",
            "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
            "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
            "10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
            "11" => "Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
            "12" => "Thẻ/Tài khoản bị khóa.",
            "13" => "Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
            "24" => "Quý khách hủy giao dịch.",
            "51" => "Tài khoản không đủ số dư để thực hiện giao dịch.",
            "65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
            "75" => "Ngân hàng thanh toán đang bảo trì.",
            "79" => "Quý khách nhập sai mật khẩu thanh toán quá số lần quy định.",
            "99" => "Lỗi không xác định.",
            _ => $"Giao dịch thất bại. Mã lỗi: {responseCode}"
        };
    }
}
