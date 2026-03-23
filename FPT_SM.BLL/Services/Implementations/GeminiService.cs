using System.Text;
using System.Text.Json;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FPT_SM.BLL.Services.Implementations;

public class GeminiService : IGeminiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GeminiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<string> GenerateContentAsync(string systemPrompt, string userMessage)
    {
        try
        {
            var apiKey = _config["GeminiAI:ApiKey"] ?? "";
            var model = _config["GeminiAI:Model"] ?? "gemini-1.5-flash";
            var baseUrl = _config["GeminiAI:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models";

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API key không được cấu hình");
                throw new Exception("API key không được cấu hình");
            }

            var url = $"{baseUrl}/{model}:generateContent?key={apiKey}";
            _logger.LogInformation($"Gemini API URL: {url}");

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt + "\n\n" + userMessage }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            _logger.LogInformation("Đang gửi yêu cầu đến Gemini API...");
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Gemini API lỗi: {response.StatusCode}\nNội dung phản hồi: {errorContent}");
                throw new Exception($"Gemini API lỗi: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Phản hồi từ Gemini: {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);

            // Kiểm tra xem có candidates không
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                _logger.LogError("Không có candidates trong phản hồi từ Gemini");
                throw new Exception("Không có candidates trong phản hồi từ Gemini");
            }

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Không có phản hồi.";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi trong GeminiService.GenerateContentAsync: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    public async Task<string> AnalyzeGradeAsync(AnalyzeGradeDto dto, string templateText)
    {
        var systemPrompt = "Bạn là chuyên gia phân tích học tập. Hãy phân tích kết quả học tập của sinh viên và đưa ra nhận xét chi tiết, gợi ý cải thiện bằng tiếng Việt.";

        var userMessage = $"""
            {templateText}

            Thông tin sinh viên:
            - Họ tên: {dto.StudentName}
            - MSSV: {dto.RollNumber ?? "N/A"}
            - Môn học: {dto.SubjectName}
            - Học kỳ: {dto.SemesterName}
            - Điểm giữa kỳ: {(dto.Midterm.HasValue ? dto.Midterm.Value.ToString("F1") : "Chưa có")}
            - Điểm cuối kỳ: {(dto.Final.HasValue ? dto.Final.Value.ToString("F1") : "Chưa có")}
            - GPA: {(dto.GPA.HasValue ? dto.GPA.Value.ToString("F2") : "Chưa có")}
            - Kết quả: {(dto.IsPassed ? "ĐẠT" : "KHÔNG ĐẠT")}

            Hãy phân tích và đưa ra nhận xét, gợi ý cải thiện phù hợp.
            """;

        return await GenerateContentAsync(systemPrompt, userMessage);
    }
}
