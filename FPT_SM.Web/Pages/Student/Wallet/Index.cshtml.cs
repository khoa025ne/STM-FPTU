using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FPT_SM.Web.Pages.Student.Wallet;

public class IndexModel : BasePageModel
{
    private readonly IWalletService _walletService;
    private readonly IPaymentService _paymentService;
    private readonly IHubContext<NotificationHub> _hub;

    public IndexModel(
        IWalletService walletService,
        IPaymentService paymentService,
        IHubContext<NotificationHub> hub)
    {
        _walletService = walletService;
        _paymentService = paymentService;
        _hub = hub;
    }

    public WalletDto? Wallet { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();
    public int TotalTransactions { get; set; }
    public decimal TotalDepositsAmount { get; set; }
    public decimal TotalPaymentsAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;
    public int CurrentPage => Page > 0 ? Page : 1;

    public int PageSize { get; set; } = 10;

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Ví tiền";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;

        var userId = CurrentUserId!.Value;
        Wallet = await _walletService.GetWalletAsync(userId);
        Transactions = await _walletService.GetTransactionsAsync(userId, CurrentPage, PageSize);
        TotalTransactions = await _walletService.GetTransactionCountAsync(userId);
        
        // Get totals from ALL transactions, not just current page
        var allTransactions = await _walletService.GetTransactionsAsync(userId, 1, int.MaxValue);
        TotalDepositsAmount = allTransactions
            .Where(t => (t.Type == "Deposit" || t.Type == "Refund") && t.Status == "Success")
            .Sum(t => t.Amount);
        TotalPaymentsAmount = allTransactions
            .Where(t => t.Type == "TuitionPayment" && t.Status == "Success")
            .Sum(t => t.Amount);

        if (TempData.ContainsKey("SuccessMessage"))
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
        if (TempData.ContainsKey("ErrorMessage"))
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];

        return Page();
    }

    public async Task<IActionResult> OnPostDepositAsync(decimal amount)
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        var userId = CurrentUserId!.Value;
        var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Create payment URL with full validation and transaction tracking
        var result = await _paymentService.CreatePaymentUrlAsync(userId, amount, ipAddr);

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể tạo liên kết thanh toán.";
            return RedirectToPage();
        }

        // Store reference code in session for tracking (optional)
        HttpContext.Session.SetString("PaymentReferenceCode", result.Data!.ReferenceCode);

        return Redirect(result.Data.PaymentUrl);
    }
}
