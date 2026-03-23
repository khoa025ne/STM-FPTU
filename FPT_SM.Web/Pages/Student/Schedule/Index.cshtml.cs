using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Schedule;

public class IndexModel : BasePageModel
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ISemesterService _semesterService;

    public IndexModel(
        IEnrollmentService enrollmentService,
        ISemesterService semesterService)
    {
        _enrollmentService = enrollmentService;
        _semesterService = semesterService;
    }

    public List<EnrollmentDto> MyEnrollments { get; set; } = new();
    public List<SemesterDto> Semesters { get; set; } = new();
    public SemesterDto? SelectedSemester { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SemesterId { get; set; }

    public Dictionary<int, string> DayMap = new()
    {
        { 2, "Thứ Hai" },
        { 3, "Thứ Ba" },
        { 4, "Thứ Tư" },
        { 5, "Thứ Năm" },
        { 6, "Thứ Sáu" },
        { 7, "Thứ Bảy" }
    };

    // Color palette for subjects (6 subjects max per day)
    private static readonly string[] SubjectColors = new[]
    {
        "from-blue-400 to-blue-600",      // Blue
        "from-purple-400 to-purple-600",  // Purple
        "from-pink-400 to-pink-600",      // Pink
        "from-green-400 to-green-600",    // Green
        "from-yellow-400 to-yellow-600",  // Yellow
        "from-indigo-400 to-indigo-600"   // Indigo
    };

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Lịch học";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;
        ViewData["AvatarUrl"] = CurrentAvatarUrl;

        var userId = CurrentUserId!.Value;

        // Get semesters and enrollments
        Semesters = await _semesterService.GetAllAsync();

        // Default to ongoing semester
        SelectedSemester = await _semesterService.GetOngoingAsync();
        if (SelectedSemester == null && Semesters.Any())
        {
            SelectedSemester = Semesters.FirstOrDefault();
        }

        SemesterId ??= SelectedSemester?.Id;

        // Get enrollments
        if (SemesterId.HasValue)
        {
            MyEnrollments = await _enrollmentService.GetByStudentAsync(userId, SemesterId.Value);
        }
        else
        {
            MyEnrollments = await _enrollmentService.GetByStudentAsync(userId);
        }

        // Filter only active enrollments (Paid - status 2)
        // Hide completed courses (status 3) from schedule
        MyEnrollments = MyEnrollments.Where(e => e.Status == 2).ToList();

        return Page();
    }

    /// <summary>
    /// Get color for subject based on index
    /// </summary>
    public string GetSubjectColor(int index)
    {
        return SubjectColors[index % SubjectColors.Length];
    }

    /// <summary>
    /// Group enrollments by day of week
    /// </summary>
    public Dictionary<int, List<EnrollmentDto>> GetEnrollmentsByDay()
    {
        var grouped = new Dictionary<int, List<EnrollmentDto>>();
        foreach (var day in DayMap.Keys)
        {
            grouped[day] = MyEnrollments
                .Where(e => e.DayName == DayMap[day])
                .OrderBy(e => e.StartTime)
                .ToList();
        }
        return grouped;
    }

    /// <summary>
    /// Get all unique time slots across all enrollments
    /// </summary>
    public List<(int index, TimeSpan time, string displayTime)> GetTimeSlots()
    {
        if (!MyEnrollments.Any())
            return new List<(int, TimeSpan, string)>();

        var times = new SortedSet<TimeSpan>();
        foreach (var enrollment in MyEnrollments)
        {
            times.Add(enrollment.StartTime);
        }

        return times.Select((t, i) => (i, t, $"{t.Hours:D2}:{t.Minutes:D2}"))
                    .ToList();
    }
}
