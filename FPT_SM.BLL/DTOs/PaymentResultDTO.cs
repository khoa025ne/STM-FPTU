namespace FPT_SM.BLL.DTOs;

public class PaymentResultDTO
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public string ReferenceCode { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string VnPayTransactionNo { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public DateTime ProcessedAt { get; set; }
    public decimal WalletBalance { get; set; }
}
