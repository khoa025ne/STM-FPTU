using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FPT_SM.Web.Controllers;

/// <summary>
/// API endpoints for payment processing (VNPay callbacks and IPN)
/// </summary>
[ApiController]
[Route("api/payment")]
public class PaymentApiController : ControllerBase
{
    private readonly IPaymentCallbackService _paymentCallbackService;
    private readonly ILogger<PaymentApiController> _logger;

    public PaymentApiController(
        IPaymentCallbackService paymentCallbackService,
        ILogger<PaymentApiController> logger)
    {
        _paymentCallbackService = paymentCallbackService;
        _logger = logger;
    }

    /// <summary>
    /// Handle VNPay IPN (Instant Payment Notification) - backend callback
    /// VNPay gọi endpoint này khi có transaction, không cần response UI
    /// </summary>
    [HttpGet("ipn")]
    [HttpPost("ipn")]
    public async Task<IActionResult> HandleIpn()
    {
        try
        {
            _logger.LogInformation("[IPN] 📨 Nhận yêu cầu IPN từ VNPay");

            // Convert query parameters to dictionary
            var queryDict = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            // Log all parameters
            var paramLog = string.Join(", ", queryDict.Select(kv => $"{kv.Key}={kv.Value?.Substring(0, Math.Min(20, kv.Value?.Length ?? 0)) ?? ""}"));
            _logger.LogInformation($"[IPN] 📋 Parameters: {paramLog}");

            // Process IPN callback
            var result = await _paymentCallbackService.ProcessIpnAsync(queryDict);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[IPN] ✅ IPN xử lý thành công");
                // VNPay expects 0 for success
                return Ok(new { RespCode = 0, Message = "Thành công" });
            }
            else
            {
                _logger.LogError($"[IPN] ❌ IPN xử lý thất bại: {result.Message}");
                // VNPay expects non-0 for failure
                return Ok(new { RespCode = 1, Message = result.Message ?? "Lỗi xử lý" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[IPN] ❌ Exception: {ex.Message}\n{ex.StackTrace}");
            return Ok(new { RespCode = 1, Message = "Lỗi server" });
        }
    }
}
