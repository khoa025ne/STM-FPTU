namespace FPT_SM.BLL.DTOs;

public class QuizDto
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPublishResult { get; set; }
    public string? QuizType { get; set; }
    public int Status { get; set; }
    public string StatusName => Status == 1 ? "Nháp" : "Đã công bố";
    public int QuestionCount { get; set; }
    public bool IsActive => DateTime.Now >= StartTime && DateTime.Now <= EndTime;
    public bool IsUpcoming => DateTime.Now < StartTime;
    public bool IsExpired => DateTime.Now > EndTime;
    public bool HasSubmitted { get; set; }
    public decimal? StudentScore { get; set; }
}

public class CreateQuizDto
{
    public int ClassId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public bool IsPublishResult { get; set; }
    public string? QuizType { get; set; }
    // 1=Draft, 2=Published
    public int Status { get; set; } = 1;
    public List<CreateQuizQuestionDto> Questions { get; set; } = new();
}

public class CreateQuizQuestionDto
{
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = "SingleChoice";
    public decimal Points { get; set; } = 1;
    public int OrderIndex { get; set; }
    public List<CreateAnswerOptionDto> Options { get; set; } = new();
}

public class CreateAnswerOptionDto
{
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class QuizDetailDto : QuizDto
{
    public List<QuizQuestionDto> Questions { get; set; } = new();
}

public class QuizQuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public decimal Points { get; set; }
    public int OrderIndex { get; set; }
    public List<QuizAnswerOptionDto> Options { get; set; } = new();
}

public class QuizAnswerOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class SubmitQuizDto
{
    public int SubmissionId { get; set; }
    public List<SubmitAnswerDto> Answers { get; set; } = new();
}

public class SubmitAnswerDto
{
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public List<int> SelectedOptionIds { get; set; } = new();
}

public class QuizResultDto
{
    public int SubmissionId { get; set; }
    public string QuizTitle { get; set; } = null!;
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public List<QuizAnswerResultDto> AnswerResults { get; set; } = new();
}

public class QuizAnswerResultDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public decimal Points { get; set; }
    public decimal ScoreEarned { get; set; }
    public bool? IsCorrect { get; set; }
    public string? StudentAnswer { get; set; }
    public List<QuizAnswerOptionDto> Options { get; set; } = new();
}

public class QuizSubmissionReportDto
{
    public int SubmissionId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public decimal Score { get; set; }
    public int AttemptCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = null!;
}
