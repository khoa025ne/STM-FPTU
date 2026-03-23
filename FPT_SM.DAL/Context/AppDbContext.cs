using FPT_SM.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.DAL.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Prerequisite> Prerequisites { get; set; }
    public DbSet<Slot> Slots { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<AcademicAnalysis> AcademicAnalyses { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<ChatLog> ChatLogs { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<GradeAnalysisTemplate> GradeAnalysisTemplates { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuizAnswerOption> QuizAnswerOptions { get; set; }
    public DbSet<QuizSubmission> QuizSubmissions { get; set; }
    public DbSet<QuizStudentAnswer> QuizStudentAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prerequisite self-referencing Subject
        modelBuilder.Entity<Prerequisite>()
            .HasOne(p => p.Subject)
            .WithMany(s => s.Prerequisites)
            .HasForeignKey(p => p.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prerequisite>()
            .HasOne(p => p.PrerequisiteSubject)
            .WithMany(s => s.IsPrerequisiteFor)
            .HasForeignKey(p => p.PrerequisiteSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Class: Teacher is a User
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Teacher)
            .WithMany(u => u.TeachingClasses)
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Grade: 1-1 with Enrollment
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Enrollment)
            .WithOne(e => e.Grade)
            .HasForeignKey<Grade>(g => g.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // AcademicAnalysis: 1-1 with Grade (optional)
        modelBuilder.Entity<AcademicAnalysis>()
            .HasOne(a => a.Grade)
            .WithOne(g => g.AcademicAnalysis)
            .HasForeignKey<AcademicAnalysis>(a => a.GradeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AcademicAnalysis>()
            .HasOne(a => a.Student)
            .WithMany(u => u.AcademicAnalyses)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AcademicAnalysis>()
            .HasOne(a => a.Class)
            .WithMany(c => c.AcademicAnalyses)
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AcademicAnalysis>()
            .HasOne(a => a.Semester)
            .WithMany()
            .HasForeignKey(a => a.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // QuizSubmission: Student
        modelBuilder.Entity<QuizSubmission>()
            .HasOne(s => s.Student)
            .WithMany(u => u.QuizSubmissions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enrollment: restrict cascades
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Semester)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction: User
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precision
        modelBuilder.Entity<User>().Property(u => u.WalletBalance).HasPrecision(18, 2);
        modelBuilder.Entity<Grade>().Property(g => g.MidtermScore).HasPrecision(4, 1);
        modelBuilder.Entity<Grade>().Property(g => g.FinalScore).HasPrecision(4, 1);
        modelBuilder.Entity<Grade>().Property(g => g.GPA).HasPrecision(4, 2);
        modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Subject>().Property(s => s.Price).HasPrecision(18, 2);
        modelBuilder.Entity<GradeAnalysisTemplate>().Property(t => t.MinGPA).HasPrecision(4, 2);
        modelBuilder.Entity<GradeAnalysisTemplate>().Property(t => t.MaxGPA).HasPrecision(4, 2);
        modelBuilder.Entity<QuizQuestion>().Property(q => q.Points).HasPrecision(5, 2);
        modelBuilder.Entity<QuizSubmission>().Property(q => q.TotalScore).HasPrecision(6, 2);
        modelBuilder.Entity<QuizStudentAnswer>().Property(q => q.ScoreEarned).HasPrecision(5, 2);
    }
}
