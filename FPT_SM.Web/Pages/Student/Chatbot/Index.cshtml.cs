using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FPT_SM.Web.Pages.Student.Chatbot;

public class IndexModel : BasePageModel
{
    private readonly IChatbotService _chatbotService;
    private readonly ISuggestionService _suggestionService;

    public IndexModel(IChatbotService chatbotService, ISuggestionService suggestionService)
    {
        _chatbotService = chatbotService;
        _suggestionService = suggestionService;
    }

    public List<ChatMessageDto> History { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return auth;

        ViewData["Title"] = "Trợ lý AI FPT";
        ViewData["FullName"] = CurrentFullName;
        ViewData["UserInitial"] = CurrentFullName?.FirstOrDefault().ToString() ?? "S";
        ViewData["UserId"] = CurrentUserId;

        History = await _chatbotService.GetHistoryAsync(CurrentUserId!.Value, 50);
        Suggestions = await _suggestionService.GenerateSuggestionsAsync(CurrentUserId!.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostSendMessageAsync([FromBody] SendMessageDto dto)
    {
        var auth = RequireRole("Student");
        if (auth != null) return new JsonResult(new { error = "Unauthorized" });

        if (string.IsNullOrWhiteSpace(dto?.Message))
            return new JsonResult(new { error = "Tin nhắn không được để trống." });

        var response = await _chatbotService.ProcessMessageAsync(CurrentUserId!.Value, dto.Message);
        return new JsonResult(new { response });
    }

    /// <summary>
    /// Endpoint to get fresh suggestions (called from frontend dynamically)
    /// </summary>
    public async Task<IActionResult> OnGetSuggestionsAsync()
    {
        var auth = RequireRole("Student");
        if (auth != null) return new JsonResult(new { error = "Unauthorized" });

        var suggestions = await _suggestionService.GenerateSuggestionsAsync(CurrentUserId!.Value);
        return new JsonResult(new { suggestions });
    }
}
