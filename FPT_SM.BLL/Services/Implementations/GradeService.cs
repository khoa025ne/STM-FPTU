using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.BLL.Services.Implementations;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IAcademicAnalysisRepository _analysisRepo;
    private readonly IGeminiService _geminiService;
    private readonly AppDbContext _context;

    public GradeService(
        IGradeRepository gradeRepo,
        IEnrollmentRepository enrollmentRepo,
        IAcademicAnalysisRepository analysisRepo,
        IGeminiService geminiService,
        AppDbContext context)
    {
        _gradeRepo = gradeRepo;
        _enrollmentRepo = enrollmentRepo;
        _analysisRepo = analysisRepo;
        _geminiService = geminiService;
        _context = context;
    }

    public async Task<GradeSheetDto?> GetGradeSheetAsync(int classId)
    {
        var cls = await _context.Classes
            .Include(c => c.Subject)
            .Include(c => c.Semester)
            .FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null) return null;

        // Ensure all enrollments have grades
        var enrollments = await _context.Enrollments
            .Where(e => e.ClassId == classId && e.Status != 4)
            .ToListAsync();

        foreach (var enrollment in enrollments)
        {
            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.EnrollmentId == enrollment.Id);

            if (existingGrade == null)
            {
                _context.Grades.Add(new Grade
                {
                    EnrollmentId = enrollment.Id,
                    Status = 1
                });
            }
        }
        await _context.SaveChangesAsync();

        var grades = await _gradeRepo.GetByClassAsync(classId);
        var gradeDtos = grades.Select(g => MapToDto(g)).ToList();

        return new GradeSheetDto
        {
            ClassId = classId,
            ClassName = cls.Name,
            SubjectName = cls.Subject?.Name ?? "",
            SemesterName = cls.Semester?.Name ?? "",
            Grades = gradeDtos
        };
    }

    public async Task<List<GradeDto>> GetByStudentAsync(int studentId)
    {
        var grades = await _gradeRepo.GetByStudentAsync(studentId);
        return grades.Select(MapToDto).ToList();
    }

    public async Task<ServiceResult<GradeDto>> UpdateGradeAsync(UpdateGradeDto dto, int teacherId)
    {
        var grade = await _gradeRepo.GetByIdAsync(dto.GradeId);
        if (grade == null) return ServiceResult<GradeDto>.Failure("Không tìm thấy điểm.");

        var enrollment = await _context.Enrollments
            .Include(e => e.Class)
            .FirstOrDefaultAsync(e => e.Id == grade.EnrollmentId);

        if (enrollment?.Class == null)
            return ServiceResult<GradeDto>.Failure("Không tìm thấy thông tin lớp học của điểm này.");

        if (enrollment.Class.TeacherId != teacherId)
            return ServiceResult<GradeDto>.Failure("Bạn không có quyền chấm điểm cho lớp này.");

        if (grade.Status == 2)
            return ServiceResult<GradeDto>.Failure("Điểm đã công bố. Hãy bấm 'Mở chấm lại' để chỉnh sửa.");

        if (dto.MidtermScore.HasValue && (dto.MidtermScore < 0 || dto.MidtermScore > 10))
            return ServiceResult<GradeDto>.Failure("Điểm giữa kỳ phải trong khoảng từ 0 đến 10.");

        if (dto.FinalScore.HasValue && (dto.FinalScore < 0 || dto.FinalScore > 10))
            return ServiceResult<GradeDto>.Failure("Điểm cuối kỳ phải trong khoảng từ 0 đến 10.");

        grade.MidtermScore = dto.MidtermScore;
        grade.FinalScore = dto.FinalScore;
        if (grade.Status == 0)
        {
            grade.Status = 1;
        }

        if (dto.MidtermScore.HasValue && dto.FinalScore.HasValue)
        {
            grade.GPA = Math.Round(dto.MidtermScore.Value * 0.4m + dto.FinalScore.Value * 0.6m, 2);
            grade.IsPassed = grade.GPA >= 5 && dto.FinalScore.Value >= 4;
        }

        await _gradeRepo.UpdateAsync(grade);

        var updated = await _gradeRepo.GetByEnrollmentAsync(grade.EnrollmentId);
        return ServiceResult<GradeDto>.Success(MapToDto(updated!), "Cập nhật điểm thành công!");
    }

    public async Task<ServiceResult> PublishGradesAsync(int classId, int teacherId)
    {
        var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null)
            return ServiceResult.Failure("Không tìm thấy lớp học.");

        if (cls.TeacherId != teacherId)
            return ServiceResult.Failure("Bạn không có quyền công bố điểm của lớp này.");

        var grades = await _gradeRepo.GetByClassAsync(classId);
        var gradeList = grades.ToList();
        if (!gradeList.Any()) return ServiceResult.Failure("Không có điểm để công bố.");

        var incompleteCount = gradeList.Count(g => !g.MidtermScore.HasValue || !g.FinalScore.HasValue);
        if (incompleteCount > 0)
        {
            return ServiceResult.Failure($"Còn {incompleteCount} sinh viên chưa nhập đủ điểm giữa kỳ/cuối kỳ.");
        }

        foreach (var grade in gradeList)
        {
            grade.Status = 2;
            grade.PublishedAt = DateTime.Now;
            await _gradeRepo.UpdateAsync(grade);
        }

        return ServiceResult.Success($"Đã công bố {gradeList.Count} điểm thành công!");
    }

    public async Task<ServiceResult> ReopenGradesAsync(int classId, int teacherId)
    {
        var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null)
            return ServiceResult.Failure("Không tìm thấy lớp học.");

        if (cls.TeacherId != teacherId)
            return ServiceResult.Failure("Bạn không có quyền mở lại điểm của lớp này.");

        var grades = (await _gradeRepo.GetByClassAsync(classId)).ToList();
        if (!grades.Any())
            return ServiceResult.Failure("Không có điểm để mở lại.");

        var reopened = 0;
        foreach (var grade in grades.Where(g => g.Status == 2))
        {
            grade.Status = 3;
            grade.PublishedAt = null;
            await _gradeRepo.UpdateAsync(grade);
            reopened++;
        }

        if (reopened == 0)
            return ServiceResult.Success("Lớp này đang ở trạng thái có thể chỉnh sửa.");

        return ServiceResult.Success($"Đã mở chấm lại {reopened} bản ghi điểm.");
    }

    public async Task<ServiceResult<AcademicAnalysisDto>> AnalyzeWithAIAsync(AnalyzeGradeDto dto)
    {
        var templates = await _analysisRepo.GetActiveTemplatesAsync();
        var template = templates.FirstOrDefault(t =>
            dto.GPA.HasValue && dto.GPA >= t.MinGPA && dto.GPA <= t.MaxGPA)
            ?? templates.FirstOrDefault();

        var templateText = template?.TemplateText
            ?? "Hãy phân tích kết quả học tập của sinh viên và đưa ra nhận xét, gợi ý cải thiện.";

        string analysisText;
        try
        {
            analysisText = await _geminiService.AnalyzeGradeAsync(dto, templateText);
        }
        catch
        {
            analysisText = $"Phân tích điểm của {dto.StudentName}: GPA={dto.GPA:F2}, môn {dto.SubjectName}. " +
                           (dto.IsPassed ? "Đạt yêu cầu." : "Chưa đạt yêu cầu, cần cải thiện.");
        }

        var existing = await _analysisRepo.GetLatestForStudentClassAsync(dto.StudentId, dto.ClassId);
        AcademicAnalysis analysis;
        if (existing != null)
        {
            existing.AnalysisText = analysisText;
            existing.CreatedAt = DateTime.Now;
            existing.TriggeredByTeacherId = dto.TeacherId;
            await _analysisRepo.UpdateAsync(existing);
            analysis = existing;
        }
        else
        {
            analysis = new AcademicAnalysis
            {
                StudentId = dto.StudentId,
                ClassId = dto.ClassId,
                SemesterId = dto.SemesterId,
                GradeId = dto.GradeId > 0 ? dto.GradeId : null,
                AnalysisText = analysisText,
                CreatedAt = DateTime.Now,
                TriggeredByTeacherId = dto.TeacherId
            };
            await _analysisRepo.AddAsync(analysis);
        }

        var student = await _context.Users.FindAsync(dto.StudentId);
        var cls = await _context.Classes.Include(c => c.Subject).Include(c => c.Semester)
            .FirstOrDefaultAsync(c => c.Id == dto.ClassId);

        return ServiceResult<AcademicAnalysisDto>.Success(new AcademicAnalysisDto
        {
            Id = analysis.Id,
            StudentId = dto.StudentId,
            StudentName = dto.StudentName,
            SubjectName = cls?.Subject?.Name ?? dto.SubjectName,
            SemesterName = cls?.Semester?.Name ?? dto.SemesterName,
            AnalysisText = analysisText,
            CreatedAt = analysis.CreatedAt
        }, "Phân tích AI thành công!");
    }

    public async Task<ServiceResult> AnalyzeClassWithAIAsync(int classId, int teacherId)
    {
        var sheet = await GetGradeSheetAsync(classId);
        if (sheet == null) return ServiceResult.Failure("Không tìm thấy lớp học.");

        var cls = await _context.Classes
            .Include(c => c.Semester)
            .FirstOrDefaultAsync(c => c.Id == classId);

        foreach (var grade in sheet.Grades)
        {
            var dto = new AnalyzeGradeDto
            {
                GradeId = grade.Id,
                StudentId = grade.StudentId,
                StudentName = grade.StudentName,
                RollNumber = grade.StudentRollNumber,
                SubjectName = sheet.SubjectName,
                SemesterName = sheet.SemesterName,
                Midterm = grade.MidtermScore,
                Final = grade.FinalScore,
                GPA = grade.GPA,
                IsPassed = grade.IsPassed,
                ClassId = classId,
                SemesterId = cls?.SemesterId ?? 0,
                TeacherId = teacherId
            };
            await AnalyzeWithAIAsync(dto);
        }

        return ServiceResult.Success($"Đã phân tích AI cho {sheet.Grades.Count} sinh viên!");
    }

    public async Task<List<AcademicAnalysisDto>> GetStudentAnalysesAsync(int studentId)
    {
        var analyses = await _analysisRepo.GetByStudentAsync(studentId);
        return analyses.Select(a => new AcademicAnalysisDto
        {
            Id = a.Id,
            StudentId = a.StudentId,
            StudentName = a.Student?.FullName ?? "",
            SubjectName = a.Class?.Subject?.Name ?? "",
            SemesterName = a.Semester?.Name ?? "",
            AnalysisText = a.AnalysisText,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    private GradeDto MapToDto(Grade g)
    {
        var e = g.Enrollment;
        return new GradeDto
        {
            Id = g.Id,
            EnrollmentId = g.EnrollmentId,
            StudentId = e?.StudentId ?? 0,
            StudentName = e?.Student?.FullName ?? "",
            StudentRollNumber = e?.Student?.RollNumber,
            ClassId = e?.ClassId ?? 0,
            SemesterId = e?.SemesterId ?? 0,
            SubjectName = e?.Class?.Subject?.Name ?? "",
            SubjectCode = e?.Class?.Subject?.Code ?? "",
            SemesterName = e?.Semester?.Name ?? "",
            MidtermScore = g.MidtermScore,
            FinalScore = g.FinalScore,
            GPA = g.GPA,
            Status = g.Status,
            PublishedAt = g.PublishedAt,
            IsPassed = g.IsPassed,
            AIAnalysis = g.AcademicAnalysis?.AnalysisText
        };
    }
}
