using System.Security.Cryptography;
using System.Text;
using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FPT_SM.BLL.Services.Implementations;

/// <summary>
/// Legacy wrapper for backward compatibility.
/// Use IPaymentService and IPaymentCallbackService directly for new code.
/// </summary>
public class VnPayService : IVnPayService
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentCallbackService _callbackService;

    public VnPayService(
        IPaymentService paymentService,
        IPaymentCallbackService callbackService)
    {
        _paymentService = paymentService;
        _callbackService = callbackService;
    }

    /// <summary>
    /// DEPRECATED: Use CreatePaymentUrlAsync instead.
    /// Kept only for backward compatibility with synchronous consumers.
    /// </summary>
    public string CreatePaymentUrl(string ipAddress, VnPayRequestDto dto)
    {
        // Blocking call - avoid in new code
        var result = CreatePaymentUrlAsync(dto.UserId, dto.Amount, ipAddress)
            .GetAwaiter().GetResult();

        return result.IsSuccess && result.Data != null
            ? result.Data.PaymentUrl
            : "";
    }

    /// <summary>
    /// Create payment URL with full validation and transaction tracking.
    /// </summary>
    public async Task<ServiceResult<DepositResponseDTO>> CreatePaymentUrlAsync(
        int userId,
        decimal amount,
        string ipAddress)
    {
        return await _paymentService.CreatePaymentUrlAsync(userId, amount, ipAddress);
    }

    /// <summary>
    /// Process VNPay callback from Return URL.
    /// </summary>
    public async Task<ServiceResult<decimal>> ProcessCallbackAsync(IDictionary<string, string> query)
    {
        var result = await _callbackService.ProcessCallbackAsync(query);

        return result.IsSuccess
            ? ServiceResult<decimal>.Success(result.Data?.Amount ?? 0, result.Data?.Message ?? "Success")
            : ServiceResult<decimal>.Failure(result.Message ?? "Payment failed");
    }

    /// <summary>
    /// Process VNPay IPN (Instant Payment Notification) from server-to-server callback.
    /// </summary>
    public async Task<ServiceResult<decimal>> ProcessIpnAsync(IDictionary<string, string> query)
    {
        var result = await _callbackService.ProcessIpnAsync(query);

        return result.IsSuccess
            ? ServiceResult<decimal>.Success(result.Data?.Amount ?? 0, result.Data?.Message ?? "Success")
            : ServiceResult<decimal>.Failure(result.Message ?? "IPN processing failed");
    }
}

