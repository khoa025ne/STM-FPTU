using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IQuizService
{
    Task<List<QuizDto>> GetByClassAsync(int classId);
    Task<List<QuizDto>> GetForStudentAsync(int studentId, int? semesterId = null);
    Task<QuizDetailDto?> GetDetailAsync(int quizId, int? studentId = null);
    Task<ServiceResult<QuizDto>> CreateAsync(CreateQuizDto dto);
    Task<ServiceResult<QuizDto>> PublishAsync(int quizId);
    Task<ServiceResult<int>> StartAttemptAsync(int quizId, int studentId);
    Task<ServiceResult<QuizResultDto>> SubmitAsync(SubmitQuizDto dto, int studentId);
    Task<QuizResultDto?> GetResultAsync(int submissionId, int studentId);
    Task<ServiceResult> DeleteAsync(int quizId);
    Task<List<QuizSubmissionReportDto>> GetSubmissionsReportAsync(int quizId, int classId);
}
