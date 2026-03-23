namespace FPT_SM.DAL.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    // Deposit, TuitionPayment, Refund
    public string Type { get; set; } = null!;
    // Success, Failed, Pending
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public string? VnPayTransactionId { get; set; }
    public string? OrderId { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User User { get; set; } = null!;
}
