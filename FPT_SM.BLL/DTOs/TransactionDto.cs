namespace FPT_SM.BLL.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Type { get; set; } = null!;
    public string TypeVN => Type switch
    {
        "Deposit" => "Nạp tiền","TuitionPayment" => "Thanh toán học phí","Refund" => "Hoàn tiền",_ => Type
    };
    public string Status { get; set; } = null!;
    public string StatusVN => Status switch
    {
        "Success" => "Thành công","Failed" => "Thất bại","Pending" => "Đang xử lý",_ => Status
    };
    public string StatusColor => Status switch
    {
        "Success" => "teal","Failed" => "red","Pending" => "amber",_ => "gray"
    };
    public string? Description { get; set; }
    public string? VnPayTransactionId { get; set; }
    public string? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VnPayRequestDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string OrderId { get; set; } = null!;
    public string Description { get; set; } = null!;
}

public class WalletDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public decimal Balance { get; set; }
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}
