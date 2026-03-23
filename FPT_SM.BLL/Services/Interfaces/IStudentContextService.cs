using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IStudentContextService
{
    /// <summary>
    /// Fetch comprehensive student context data for AI chatbot personalization
    /// Includes enrollments, grades, attendance, quizzes, and wallet balance
    /// </summary>
    Task<StudentContextDto> GetStudentContextAsync(int userId);
}
