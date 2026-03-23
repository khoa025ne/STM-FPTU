using FPT_SM.BLL.Services.Implementations;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Repositories.Implementations;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FPT_SM.BLL.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISemesterRepository, SemesterRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IGradeRepository, GradeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IChatLogRepository, ChatLogRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IAcademicAnalysisRepository, AcademicAnalysisRepository>();
        services.AddScoped<IQuizRepository, QuizRepository>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentCallbackService, PaymentCallbackService>();
        services.AddScoped<IVnPayService, VnPayService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IStudentContextService, StudentContextService>();
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<IGeminiService, GeminiService>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<IChatSessionService, ChatSessionService>();

        return services;
    }
}
