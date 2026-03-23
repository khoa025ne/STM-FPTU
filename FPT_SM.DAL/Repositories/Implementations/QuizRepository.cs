using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Repositories.Implementations;

public class QuizRepository : GenericRepository<Quiz>, IQuizRepository
{
    public QuizRepository(AppDbContext context) : base(context) { }

    public async Task<Quiz?> GetWithQuestionsAsync(int id)
        => await _context.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == id);

    public async Task<IEnumerable<Quiz>> GetByClassAsync(int classId)
        => await _context.Quizzes.Where(q => q.ClassId == classId)
            .OrderByDescending(q => q.CreatedAt).ToListAsync();

    public async Task<IEnumerable<Quiz>> GetActiveForStudentAsync(int studentId)
        => await _context.Quizzes
            .Include(q => q.Class)
            .Where(q => q.Class.Enrollments.Any(e => e.StudentId == studentId && e.Status == 2) && q.Status == 2)
            .ToListAsync();

    public async Task<QuizSubmission?> GetSubmissionAsync(int quizId, int studentId)
        => await _context.QuizSubmissions.FirstOrDefaultAsync(s => s.QuizId == quizId && s.StudentId == studentId);

    public async Task<QuizSubmission?> GetSubmissionWithAnswersAsync(int submissionId)
        => await _context.QuizSubmissions
            .Include(s => s.StudentAnswers).ThenInclude(a => a.Question).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(s => s.Id == submissionId);

    public async Task AddSubmissionAsync(QuizSubmission submission)
    {
        await _context.QuizSubmissions.AddAsync(submission);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSubmissionAsync(QuizSubmission submission)
    {
        _context.QuizSubmissions.Update(submission);
        await _context.SaveChangesAsync();
    }
}
