namespace FPT_SM.BLL.DTOs;

public class DepositResponseDTO
{
    public int TransactionId { get; set; }
    public string ReferenceCode { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
