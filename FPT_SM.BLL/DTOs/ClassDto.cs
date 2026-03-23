namespace FPT_SM.BLL.DTOs;

public class ClassDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = null!;
    public int SlotId { get; set; }
    public string SlotName { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public string DayName => DayOfWeek switch
    {
        2 => "Thứ Hai",3 => "Thứ Ba",4 => "Thứ Tư",5 => "Thứ Năm",
        6 => "Thứ Sáu",7 => "Thứ Bảy",1 => "Chủ Nhật",_ => ""
    };
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string SlotTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public int Capacity { get; set; }
    public int Enrolled { get; set; }
    public int AvailableSlots => Capacity - Enrolled;
    public int Status { get; set; }
    public string StatusName => Status switch
    {
        0 => "Đã hủy",1 => "Đang mở",2 => "Đang học",3 => "Đã kết thúc",_ => "Không xác định"
    };
    public string StatusColor => Status switch
    {
        0 => "red",1 => "sky",2 => "teal",3 => "slate",_ => "gray"
    };
    public string? RoomNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClassDto
{
    public string Name { get; set; } = null!;
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    public int SemesterId { get; set; }
    public int SlotId { get; set; }
    public int Capacity { get; set; } = 30;
    public string? RoomNumber { get; set; }
}

public class UpdateClassDto
{
    public string Name { get; set; } = null!;
    public int TeacherId { get; set; }
    public int SlotId { get; set; }
    public int Capacity { get; set; }
    public string? RoomNumber { get; set; }
}

public class SlotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DayName => DayOfWeek switch
    {
        2 => "Thứ Hai",3 => "Thứ Ba",4 => "Thứ Tư",5 => "Thứ Năm",
        6 => "Thứ Sáu",7 => "Thứ Bảy",1 => "Chủ Nhật",_ => ""
    };
}
