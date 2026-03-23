namespace FPT_SM.BLL.DTOs;

public class SemesterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Status { get; set; }
    public string StatusName => Status switch
    {
        0 => "Đã đóng",
        1 => "Sắp diễn ra",
        2 => "Đang diễn ra",
        3 => "Đã kết thúc",
        _ => "Không xác định"
    };
    public string StatusColor => Status switch
    {
        0 => "red",
        1 => "sky",
        2 => "teal",
        3 => "slate",
        _ => "gray"
    };
    public int ClassCount { get; set; }
    public int EnrollmentCount { get; set; }
}

public class CreateSemesterDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateSemesterDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
