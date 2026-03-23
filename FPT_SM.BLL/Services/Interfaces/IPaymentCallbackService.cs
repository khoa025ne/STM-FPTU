using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IPaymentCallbackService
{
    /// <summary>
    /// Xử lý callback từ VNPay khi người dùng được chuyển hướng về
    /// </summary>
    Task<ServiceResult<PaymentResultDTO>> ProcessCallbackAsync(
        IDictionary<string, string> queryParams);

    /// <summary>
    /// Xử lý IPN (Instant Payment Notification) từ máy chủ VNPay
    /// </summary>
    Task<ServiceResult<PaymentResultDTO>> ProcessIpnAsync(
        IDictionary<string, string> queryParams);

    /// <summary>
    /// Xác thực chữ ký HMAC-SHA512 từ VNPay
    /// </summary>
    ServiceResult VerifySignature(
        IDictionary<string, string> parameters,
        string receivedHash);
}
