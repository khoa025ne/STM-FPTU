using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Payment;

public class CallbackModel : PageModel
{
    private readonly IPaymentCallbackService _paymentCallbackService;
    private readonly IWalletService _walletService;
    private readonly IHubContext<NotificationHub> _hub;

    public CallbackModel(
        IPaymentCallbackService paymentCallbackService,
        IWalletService walletService,
        IHubContext<NotificationHub> hub)
    {
        _paymentCallbackService = paymentCallbackService;
        _walletService = walletService;
        _hub = hub;
    }

    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var queryDict = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        var result = await _paymentCallbackService.ProcessCallbackAsync(queryDict);

        if (result.IsSuccess)
        {
            var paymentData = result.Data;
            if (paymentData != null)
            {
                Amount = paymentData.Amount;
                Message = paymentData.Message;
                ReferenceCode = paymentData.ReferenceCode;
                IsSuccess = true;

                // Broadcast wallet update via SignalR
                if (paymentData.TransactionId > 0 && paymentData.UserId > 0)
                {
                    var wallet = await _walletService.GetWalletAsync(paymentData.UserId);
                    await _hub.Clients.Group($"student_{paymentData.UserId}")
                        .SendAsync("WalletUpdated", wallet.Balance);
                }

                TempData["SuccessMessage"] = Message;
            }
        }
        else
        {
            IsSuccess = false;
            Message = result.Message ?? "Giao dịch thất bại. Vui lòng thử lại.";
            TempData["ErrorMessage"] = Message;
        }

        return Page();
    }
}
