using FPT_SM.DAL.Entities;

namespace FPT_SM.DAL.Repositories.Interfaces;

public interface IQuizRepository : IGenericRepository<Quiz>
{
    Task<Quiz?> GetWithQuestionsAsync(int id);
    Task<IEnumerable<Quiz>> GetByClassAsync(int classId);
    Task<IEnumerable<Quiz>> GetActiveForStudentAsync(int studentId);
    Task<QuizSubmission?> GetSubmissionAsync(int quizId, int studentId);
    Task<QuizSubmission?> GetSubmissionWithAnswersAsync(int submissionId);
    Task AddSubmissionAsync(QuizSubmission submission);
    Task UpdateSubmissionAsync(QuizSubmission submission);
}
