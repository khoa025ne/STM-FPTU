using ClosedXML.Excel;
using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace FPT_SM.BLL.Services.Implementations;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminOverviewDto> GetAdminOverviewAsync()
    {
        var totalStudents = await _context.Users.CountAsync(u => u.RoleId == 4);
        var totalTeachers = await _context.Users.CountAsync(u => u.RoleId == 3);
        var totalRevenue = await _context.Transactions
            .Where(t => t.Status == "Success")
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        var monthlyRevenue = await _context.Transactions
            .Where(t => t.Status == "Success" && t.CreatedAt >= DateTime.Now.AddMonths(-1))
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return new AdminOverviewDto
        {
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue
        };
    }

    public async Task<List<EnrollmentReportDto>> GetEnrollmentReportAsync(ReportFilterDto filter)
    {
        var query = _context.Enrollments
            .Include(e => e.Class).ThenInclude(c => c.Subject)
            .Include(e => e.Semester)
            .AsQueryable();

        if (filter.SemesterId.HasValue)
            query = query.Where(e => e.SemesterId == filter.SemesterId.Value);
        if (filter.ClassId.HasValue)
            query = query.Where(e => e.ClassId == filter.ClassId.Value);
        if (filter.SubjectId.HasValue)
            query = query.Where(e => e.Class.SubjectId == filter.SubjectId.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(e => e.EnrolledAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(e => e.EnrolledAt <= filter.ToDate.Value);

        var data = await query.GroupBy(e => new
        {
            e.ClassId,
            ClassName = e.Class.Name,
            SubjectCode = e.Class.Subject.Code,
            SubjectName = e.Class.Subject.Name,
            Capacity = e.Class.Capacity
        }).Select(g => new EnrollmentReportDto
        {
            ClassName = g.Key.ClassName,
            SubjectCode = g.Key.SubjectCode,
            SubjectName = g.Key.SubjectName,
            TotalEnrolled = g.Count(),
            Capacity = g.Key.Capacity
        }).ToListAsync();

        return data;
    }

    public async Task<List<GradeReportDto>> GetGradeReportAsync(ReportFilterDto filter)
    {
        var query = ApplyGradeFilter(_context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e.Class).ThenInclude(c => c.Subject), filter);
        query = query.Where(g => g.Status == 2);

        var data = await query.GroupBy(g => new
        {
            SubjectCode = g.Enrollment.Class.Subject.Code,
            SubjectName = g.Enrollment.Class.Subject.Name
        }).Select(g => new GradeReportDto
        {
            SubjectCode = g.Key.SubjectCode,
            SubjectName = g.Key.SubjectName,
            TotalStudents = g.Count(),
            PassCount = g.Count(x => x.IsPassed),
            FailCount = g.Count(x => !x.IsPassed),
            AverageGPA = g.Average(x => x.GPA)
        }).ToListAsync();

        return data;
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(ReportFilterDto filter)
    {
        var query = ApplyTransactionFilter(_context.Transactions.Where(t => t.Status == "Success"), filter);
        var transactions = await query.ToListAsync();

        var total = transactions.Sum(t => t.Amount);
        var tuition = transactions.Where(t => t.Type == "TuitionPayment").Sum(t => t.Amount);
        var deposit = transactions.Where(t => t.Type == "Deposit").Sum(t => t.Amount);

        var monthly = transactions
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenueDto
            {
                Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                Revenue = g.Sum(t => t.Amount)
            }).ToList();

        return new RevenueReportDto
        {
            TotalRevenue = total,
            TuitionRevenue = tuition,
            DepositRevenue = deposit,
            TransactionCount = transactions.Count,
            Monthly = monthly
        };
    }

    public async Task<List<SemesterEnrollmentDto>> GetEnrollmentTrendAsync(ReportFilterDto? filter = null)
    {
        var query = _context.Enrollments
            .Include(e => e.Semester)
            .AsQueryable();

        if (filter?.SemesterId.HasValue == true)
            query = query.Where(e => e.SemesterId == filter.SemesterId.Value);

        if (filter?.FromDate.HasValue == true)
            query = query.Where(e => e.EnrolledAt >= filter.FromDate.Value);
        if (filter?.ToDate.HasValue == true)
            query = query.Where(e => e.EnrolledAt <= filter.ToDate.Value);

        var data = await query.GroupBy(e => new { e.SemesterId, e.Semester.Name })
            .OrderBy(g => g.Key.SemesterId)
            .Select(g => new SemesterEnrollmentDto
            {
                SemesterName = g.Key.Name,
                EnrollmentCount = g.Count()
            }).ToListAsync();

        return data;
    }

    public async Task<List<ManagerClassStatusDto>> GetClassStatusReportAsync()
    {
        var data = await _context.Classes
            .GroupBy(c => c.Status)
            .Select(g => new ManagerClassStatusDto
            {
                Status = g.Key,
                StatusLabel = MapClassStatusLabel(g.Key),
                Count = g.Count()
            }).OrderBy(g => g.Status).ToListAsync();

        return data;
    }

    public async Task<List<ManagerTeacherReportDto>> GetTeacherWorkloadAsync()
    {
        var data = await _context.Classes
            .Include(c => c.Teacher)
            .GroupBy(c => new { c.TeacherId, TeacherName = c.Teacher.FullName })
            .Select(g => new ManagerTeacherReportDto
            {
                TeacherId = g.Key.TeacherId,
                TeacherName = g.Key.TeacherName ?? "GV chưa cập nhật",
                ClassCount = g.Count()
            }).OrderByDescending(g => g.ClassCount).ToListAsync();

        return data;
    }

    public async Task<List<ClassExportDto>> GetClassListAsync(ReportFilterDto? filter = null)
    {
        var query = _context.Classes
            .Include(c => c.Subject)
            .Include(c => c.Teacher)
            .Include(c => c.Semester)
            .AsQueryable();

        if (filter?.SemesterId.HasValue == true)
            query = query.Where(c => c.SemesterId == filter.SemesterId.Value);
        if (filter?.ClassId.HasValue == true)
            query = query.Where(c => c.Id == filter.ClassId.Value);

        var data = await query.Select(c => new ClassExportDto
        {
            ClassId = c.Id,
            ClassName = c.Name,
            SubjectName = c.Subject.Name,
            TeacherName = c.Teacher.FullName,
            StatusLabel = MapClassStatusLabel(c.Status),
            Capacity = c.Capacity,
            Enrolled = c.Enrolled,
            CreatedAt = c.CreatedAt
        }).OrderBy(c => c.SubjectName).ThenBy(c => c.ClassName).ToListAsync();

        return data;
    }

    public async Task<byte[]> ExportGradeReportToExcelAsync(ReportFilterDto filter)
    {
        var gradeQuery = ApplyGradeFilter(_context.Grades
            .Include(g => g.Enrollment).ThenInclude(e => e.Student)
            .Include(g => g.Enrollment).ThenInclude(e => e.Class).ThenInclude(c => c.Subject), filter)
            .Where(g => g.Status == 2);
        var gradeRecords = await gradeQuery.ToListAsync();

        var students = await _context.Users
            .Where(u => u.RoleId == 4)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var transactions = await ApplyTransactionFilter(_context.Transactions.Where(t => t.Status == "Success"), filter)
            .Include(t => t.User)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var studentSheet = workbook.AddWorksheet("Sinh viên");
        studentSheet.Cell(1, 1).Value = "Họ tên";
        studentSheet.Cell(1, 2).Value = "Email";
        studentSheet.Cell(1, 3).Value = "Số dư ví";
        studentSheet.Cell(1, 4).Value = "Vai trò";

        for (var i = 0; i < students.Count; i++)
        {
            var row = i + 2;
            var student = students[i];
            studentSheet.Cell(row, 1).Value = student.FullName;
            studentSheet.Cell(row, 2).Value = student.Email;
            studentSheet.Cell(row, 3).Value = student.WalletBalance;
            studentSheet.Cell(row, 4).Value = "Sinh viên";
        }

        var gradeSheet = workbook.AddWorksheet("Điểm");
        var gradeHeaders = new[] { "Sinh viên", "MSSV", "Lớp", "Môn", "Midterm", "Final", "GPA", "Trạng thái" };
        for (var i = 0; i < gradeHeaders.Length; i++)
            gradeSheet.Cell(1, i + 1).Value = gradeHeaders[i];

        for (var i = 0; i < gradeRecords.Count; i++)
        {
            var row = i + 2;
            var grade = gradeRecords[i];
            gradeSheet.Cell(row, 1).Value = grade.Enrollment.Student.FullName;
            gradeSheet.Cell(row, 2).Value = grade.Enrollment.Student.RollNumber;
            gradeSheet.Cell(row, 3).Value = grade.Enrollment.Class.Name;
            gradeSheet.Cell(row, 4).Value = grade.Enrollment.Class.Subject.Name;
            gradeSheet.Cell(row, 5).Value = grade.MidtermScore;
            gradeSheet.Cell(row, 6).Value = grade.FinalScore;
            gradeSheet.Cell(row, 7).Value = grade.GPA;
            gradeSheet.Cell(row, 8).Value = grade.Status == 2 ? "Published" : "Draft";
        }

        var revenueSheet = workbook.AddWorksheet("Doanh thu");
        var revenueHeaders = new[] { "Ngày", "Loại", "Số tiền", "Trạng thái", "Người dùng", "Ghi chú" };
        for (var i = 0; i < revenueHeaders.Length; i++)
            revenueSheet.Cell(1, i + 1).Value = revenueHeaders[i];

        for (var i = 0; i < transactions.Count; i++)
        {
            var row = i + 2;
            var txn = transactions[i];
            revenueSheet.Cell(row, 1).Value = txn.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            revenueSheet.Cell(row, 2).Value = txn.Type;
            revenueSheet.Cell(row, 3).Value = txn.Amount;
            revenueSheet.Cell(row, 4).Value = txn.Status;
            revenueSheet.Cell(row, 5).Value = txn.User.FullName;
            revenueSheet.Cell(row, 6).Value = txn.Description;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportGradeReportToPdfAsync(ReportFilterDto filter)
    {
        var overview = await GetAdminOverviewAsync();
        var revenue = await GetRevenueReportAsync(filter);
        var gradeReport = await GetGradeReportAsync(filter);
        var semesters = await GetEnrollmentTrendAsync(filter);

        var document = new SummaryReportDocument(overview, revenue, gradeReport, semesters);
        return document.GeneratePdf();
    }

    public Task<byte[]> ExportEnrollmentReportToExcelAsync(ReportFilterDto filter)
        => Task.FromResult(Array.Empty<byte>());

    public async Task<byte[]> ExportClassReportToExcelAsync(ReportFilterDto filter)
    {
        var classes = await GetClassListAsync(filter);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Danh sách lớp");
        var headers = new[] { "Tên lớp", "Môn học", "Giáo viên", "Trạng thái", "Sức chứa", "Số đăng ký", "Ngày tạo" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        for (var i = 0; i < classes.Count; i++)
        {
            var row = i + 2;
            var cls = classes[i];
            sheet.Cell(row, 1).Value = cls.ClassName;
            sheet.Cell(row, 2).Value = cls.SubjectName;
            sheet.Cell(row, 3).Value = cls.TeacherName;
            sheet.Cell(row, 4).Value = cls.StatusLabel;
            sheet.Cell(row, 5).Value = cls.Capacity;
            sheet.Cell(row, 6).Value = cls.Enrolled;
            sheet.Cell(row, 7).Value = cls.CreatedAt.ToString("dd/MM/yyyy");
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IQueryable<Grade> ApplyGradeFilter(IQueryable<Grade> query, ReportFilterDto filter)
    {
        if (filter.SemesterId.HasValue)
            query = query.Where(g => g.Enrollment.SemesterId == filter.SemesterId.Value);
        if (filter.ClassId.HasValue)
            query = query.Where(g => g.Enrollment.ClassId == filter.ClassId.Value);
        if (filter.SubjectId.HasValue)
            query = query.Where(g => g.Enrollment.Class.SubjectId == filter.SubjectId.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(g => g.Enrollment.Class.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(g => g.Enrollment.Class.CreatedAt <= filter.ToDate.Value);
        return query;
    }

    private static IQueryable<Transaction> ApplyTransactionFilter(IQueryable<Transaction> query, ReportFilterDto filter)
    {
        if (filter.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);
        if (filter.Month.HasValue)
            query = query.Where(t => t.CreatedAt.Month == filter.Month.Value);
        if (filter.Year.HasValue)
            query = query.Where(t => t.CreatedAt.Year == filter.Year.Value);
        return query;
    }

    private static string MapClassStatusLabel(int status)
        => status switch
        {
            0 => "Huỷ",
            1 => "Mở",
            2 => "Đang diễn ra",
            3 => "Kết thúc",
            _ => "Không xác định"
        };

    private class SummaryReportDocument : IDocument
    {
        private readonly AdminOverviewDto _overview;
        private readonly RevenueReportDto _revenue;
        private readonly List<GradeReportDto> _grades;
        private readonly List<SemesterEnrollmentDto> _semesters;

        public SummaryReportDocument(
            AdminOverviewDto overview,
            RevenueReportDto revenue,
            List<GradeReportDto> grades,
            List<SemesterEnrollmentDto> semesters)
        {
            _overview = overview;
            _revenue = revenue;
            _grades = grades;
            _semesters = semesters;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("BÁO CÁO TỔNG HỢP").FontSize(20).SemiBold();
                            row.ConstantColumn(150).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"));
                        });

                        column.Item().LineHorizontal(1);

                        column.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().Text($"Tổng sinh viên: {_overview.TotalStudents:N0}");
                                col.Item().Text($"Tổng giáo viên: {_overview.TotalTeachers:N0}");
                            });
                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().Text($"Tổng doanh thu: {_overview.TotalRevenue:C0}", TextStyle.Default.Color(Colors.Green.Darken2));
                                col.Item().Text($"Doanh thu tháng: {_overview.MonthlyRevenue:C0}", TextStyle.Default.Color(Colors.Blue.Darken2));
                            });
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().Text("Doanh thu theo tháng").Bold();
                                col.Item().LineHorizontal(0.5f);
                                foreach (var item in _revenue.Monthly)
                                {
                                    col.Item().Text($"{item.Month}: {item.Revenue:C0}");
                                }
                            });
                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().Text("Tỉ lệ đậu theo môn").Bold();
                                col.Item().LineHorizontal(0.5f);
                                foreach (var subject in _grades.Take(5))
                                {
                                    col.Item().Text($"{subject.SubjectCode} - {subject.PassRate:F1}% đậu");
                                }
                            });
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().Text("Số lượng đăng ký theo học kỳ").Bold();
                                col.Item().LineHorizontal(0.5f);
                                foreach (var sem in _semesters)
                                {
                                    col.Item().Text($"{sem.SemesterName}: {sem.EnrollmentCount} SV");
                                }
                            });
                        });
                    });
            });
        }
    }
}
