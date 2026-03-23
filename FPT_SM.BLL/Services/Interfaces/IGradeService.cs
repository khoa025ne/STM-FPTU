using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IGradeService
{
    Task<GradeSheetDto?> GetGradeSheetAsync(int classId);
    Task<List<GradeDto>> GetByStudentAsync(int studentId);
    Task<ServiceResult<GradeDto>> UpdateGradeAsync(UpdateGradeDto dto, int teacherId);
    Task<ServiceResult> PublishGradesAsync(int classId, int teacherId);
    Task<ServiceResult<AcademicAnalysisDto>> AnalyzeWithAIAsync(AnalyzeGradeDto dto);
    Task<ServiceResult> AnalyzeClassWithAIAsync(int classId, int teacherId);
    Task<List<AcademicAnalysisDto>> GetStudentAnalysesAsync(int studentId);
}
