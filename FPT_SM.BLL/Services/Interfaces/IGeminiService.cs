using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IGeminiService
{
    Task<string> GenerateContentAsync(string systemPrompt, string userMessage);
    Task<string> AnalyzeGradeAsync(AnalyzeGradeDto dto, string templateText);
}
