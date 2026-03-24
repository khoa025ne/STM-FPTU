namespace FPT_SM.BLL.DTOs;

public class AttendanceDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentRollNumber { get; set; }
    public DateTime SlotDate { get; set; }
    public int SessionNumber { get; set; }
    public string Status { get; set; } = "Present";
    public string StatusVN => Status switch
    {
        "Present" => "Có mặt",
        "Absent" => "Vắng mặt",
        "Late" => "Đi trễ",
        "Excused" => "Có phép",
        "Upcoming" => "Sắp tới",
        _ => Status
    };
    public string StatusColor => Status switch
    {
        "Present" => "teal",
        "Absent" => "red",
        "Late" => "amber",
        "Excused" => "sky",
        "Upcoming" => "slate",
        _ => "gray"
    };
    public string? Note { get; set; }
    public bool IsEditable { get; set; }
}

public class AttendanceSheetDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public DateTime Date { get; set; }
    public int SessionNumber { get; set; }
    public int TotalSessions { get; set; } = 20;  // Fixed at 20 sessions per semester
    public bool IsCompleted => SessionNumber > TotalSessions;  // All sessions completed
    public List<AttendanceDto> Attendances { get; set; } = new();
}

public class MarkAttendanceDto
{
    public int ClassId { get; set; }
    public DateTime Date { get; set; }
    public int SessionNumber { get; set; }
    public List<AttendanceItemDto> Items { get; set; } = new();
}

public class AttendanceItemDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string Status { get; set; } = "Present";
    public string? Note { get; set; }
}

public class StudentAttendanceSummaryDto
{
    public int EnrollmentId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    public bool IsFailedByAbsence => AbsentCount >= 5;
    public double AttendanceRate => TotalSessions > 0 ? (double)(PresentCount + LateCount + ExcusedCount) / TotalSessions * 100 : 0;
    public List<AttendanceDto> Sessions { get; set; } = new();
}

public class AttendanceHistoryDto
{
    public DateTime Date { get; set; }
    public int SessionNumber { get; set; }
    public string DisplayText => $"Buổi {SessionNumber} - {Date:dd/MM/yyyy}";
}

public class ClassAttendanceSessionDto
{
    public int SessionNumber { get; set; }
    public DateTime Date { get; set; }
    public bool IsFuture => Date.Date > DateTime.Today;
    public string DisplayText => $"Buổi {SessionNumber} - {Date:dd/MM/yyyy}";
}

