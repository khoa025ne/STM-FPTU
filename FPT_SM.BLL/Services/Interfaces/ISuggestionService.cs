namespace FPT_SM.BLL.Services.Interfaces;

/// <summary>
/// Generates dynamic, context-aware AI chatbot suggestions for students
/// based on their academic status, wallet balance, attendance, etc.
/// </summary>
public interface ISuggestionService
{
    /// <summary>
    /// Generates personalized chatbot suggestions based on student context
    /// </summary>
    /// <param name="userId">Student ID</param>
    /// <returns>List of 3-5 smart suggestions</returns>
    Task<List<string>> GenerateSuggestionsAsync(int userId);
}
