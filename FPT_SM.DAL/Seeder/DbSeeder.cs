using System.Security.Cryptography;
using System.Text;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FPT_SM.DAL.Seeder;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Id = 1, Name = "Admin", Description = "Quản trị viên hệ thống" },
                new Role { Id = 2, Name = "Manager", Description = "Quản lý học vụ" },
                new Role { Id = 3, Name = "Teacher", Description = "Giảng viên" },
                new Role { Id = 4, Name = "Student", Description = "Sinh viên" }
            );
            await context.SaveChangesAsync();
        }

        // Seed Users
        if (!await context.Users.AnyAsync())
        {
            context.Users.AddRange(
                new User
                {
                    FullName = "Quản Trị Viên",
                    Email = "admin@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "admin@fpt.edu.vn"),
                    RoleId = 1,
                    WalletBalance = 0,
                    IsActive = true
                },
                new User
                {
                    FullName = "Nguyễn Văn Manager",
                    Email = "manager@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "manager@fpt.edu.vn"),
                    RoleId = 2,
                    WalletBalance = 0,
                    IsActive = true
                },
                new User
                {
                    FullName = "Trần Thị Giảng Viên",
                    Email = "teacher01@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "teacher01@fpt.edu.vn"),
                    RoleId = 3,
                    WalletBalance = 0,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100001",
                    FullName = "Lê Văn Sinh Viên",
                    Email = "student01@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student01@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 20000000,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100002",
                    FullName = "Phạm Thị Học Trò",
                    Email = "student02@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student02@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 5000000,
                    IsActive = true
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed Semesters
        if (!await context.Semesters.AnyAsync())
        {
            context.Semesters.AddRange(
                new Semester
                {
                    Name = "Spring 2025",
                    Code = "SP25",
                    StartDate = new DateTime(2025, 1, 6),
                    EndDate = new DateTime(2025, 4, 30),
                    Status = 3 // Finished
                },
                new Semester
                {
                    Name = "Fall 2025",
                    Code = "FA25",
                    StartDate = new DateTime(2025, 9, 1),
                    EndDate = new DateTime(2026, 1, 15),
                    Status = 2 // Ongoing
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed Subjects
        if (!await context.Subjects.AnyAsync())
        {
            context.Subjects.AddRange(
                new Subject { Code = "PRX101", Name = "Lập trình C cơ bản", Credits = 3, Price = 6000000, Description = "Môn học nhập môn lập trình với ngôn ngữ C" },
                new Subject { Code = "PRO192", Name = "Lập trình OOP với Java", Credits = 3, Price = 6000000, Description = "Lập trình hướng đối tượng với Java" },
                new Subject { Code = "DBI202", Name = "Nhập môn Cơ sở dữ liệu", Credits = 3, Price = 6000000, Description = "Thiết kế và quản lý cơ sở dữ liệu" },
                new Subject { Code = "WED201", Name = "Thiết kế Web", Credits = 3, Price = 6000000, Description = "HTML, CSS, JavaScript cơ bản" },
                new Subject { Code = "MAE101", Name = "Toán cho Kỹ thuật", Credits = 3, Price = 6000000, Description = "Toán ứng dụng trong kỹ thuật phần mềm" }
            );
            await context.SaveChangesAsync();

            // Seed Prerequisite: PRO192 requires PRX101
            var prx101 = await context.Subjects.FirstAsync(s => s.Code == "PRX101");
            var pro192 = await context.Subjects.FirstAsync(s => s.Code == "PRO192");
            context.Prerequisites.Add(new Prerequisite
            {
                SubjectId = pro192.Id,
                PrerequisiteSubjectId = prx101.Id
            });
            await context.SaveChangesAsync();
        }

        // Seed Slots
        if (!await context.Slots.AnyAsync())
        {
            context.Slots.AddRange(
                new Slot { Name = "Slot 1 - Thứ 2 sáng", DayOfWeek = 2, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 30, 0) },
                new Slot { Name = "Slot 2 - Thứ 2 chiều", DayOfWeek = 2, StartTime = new TimeSpan(9, 45, 0), EndTime = new TimeSpan(11, 45, 0) },
                new Slot { Name = "Slot 3 - Thứ 4 sáng", DayOfWeek = 4, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 30, 0) },
                new Slot { Name = "Slot 4 - Thứ 4 chiều", DayOfWeek = 4, StartTime = new TimeSpan(9, 45, 0), EndTime = new TimeSpan(11, 45, 0) }
            );
            await context.SaveChangesAsync();
        }

        // Seed Classes
        if (!await context.Classes.AnyAsync())
        {
            var fa25 = await context.Semesters.FirstAsync(s => s.Code == "FA25");
            var pro192 = await context.Subjects.FirstAsync(s => s.Code == "PRO192");
            var dbi202 = await context.Subjects.FirstAsync(s => s.Code == "DBI202");
            var teacher = await context.Users.FirstAsync(u => u.Email == "teacher01@fpt.edu.vn");
            var slot1 = await context.Slots.FirstAsync(s => s.DayOfWeek == 2 && s.StartTime.Hours == 7);
            var slot3 = await context.Slots.FirstAsync(s => s.DayOfWeek == 4 && s.StartTime.Hours == 7);

            context.Classes.AddRange(
                new Class
                {
                    Name = "PRO192_C01",
                    SubjectId = pro192.Id,
                    TeacherId = teacher.Id,
                    SemesterId = fa25.Id,
                    SlotId = slot1.Id,
                    Capacity = 30,
                    Enrolled = 1,
                    Status = 2, // Ongoing
                    RoomNumber = "B2.01"
                },
                new Class
                {
                    Name = "DBI202_C01",
                    SubjectId = dbi202.Id,
                    TeacherId = teacher.Id,
                    SemesterId = fa25.Id,
                    SlotId = slot3.Id,
                    Capacity = 30,
                    Enrolled = 1,
                    Status = 2, // Ongoing
                    RoomNumber = "B2.02"
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed Enrollments + Grades + Attendances for student01
        if (!await context.Enrollments.AnyAsync())
        {
            var student01 = await context.Users.FirstAsync(u => u.Email == "student01@fpt.edu.vn");
            var fa25 = await context.Semesters.FirstAsync(s => s.Code == "FA25");
            var proClass = await context.Classes.FirstAsync(c => c.Name == "PRO192_C01");
            var dbiClass = await context.Classes.FirstAsync(c => c.Name == "DBI202_C01");

            // Enrollment PRO192
            var enrollment1 = new Enrollment
            {
                StudentId = student01.Id,
                ClassId = proClass.Id,
                SemesterId = fa25.Id,
                Status = 2,
                EnrolledAt = DateTime.Now.AddMonths(-2)
            };
            await context.Enrollments.AddAsync(enrollment1);
            await context.SaveChangesAsync();

            var grade1 = new Grade
            {
                EnrollmentId = enrollment1.Id,
                MidtermScore = 7,
                FinalScore = 8,
                GPA = 7.6m,
                IsPassed = true,
                Status = 2,
                PublishedAt = DateTime.Now.AddMonths(-1)
            };
            await context.Grades.AddAsync(grade1);

            // Enrollment DBI202
            var enrollment2 = new Enrollment
            {
                StudentId = student01.Id,
                ClassId = dbiClass.Id,
                SemesterId = fa25.Id,
                Status = 2,
                EnrolledAt = DateTime.Now.AddMonths(-2)
            };
            await context.Enrollments.AddAsync(enrollment2);
            await context.SaveChangesAsync();

            var grade2 = new Grade
            {
                EnrollmentId = enrollment2.Id,
                MidtermScore = 5,
                FinalScore = 4,
                GPA = 4.4m,
                IsPassed = false,
                Status = 1
            };
            await context.Grades.AddAsync(grade2);
            await context.SaveChangesAsync();

            // Seed 20 attendance records for each enrollment
            var now = DateTime.Now;
            var attendances = new List<Attendance>();
            for (int i = 1; i <= 20; i++)
            {
                attendances.Add(new Attendance
                {
                    EnrollmentId = enrollment1.Id,
                    SlotDate = now.AddDays(-(20 - i) * 7),
                    SessionNumber = i,
                    Status = i <= 18 ? "Present" : "Absent"
                });
                attendances.Add(new Attendance
                {
                    EnrollmentId = enrollment2.Id,
                    SlotDate = now.AddDays(-(20 - i) * 7),
                    SessionNumber = i,
                    Status = i <= 16 ? "Present" : "Absent"
                });
            }
            await context.Attendances.AddRangeAsync(attendances);
            await context.SaveChangesAsync();
        }

        // Seed GradeAnalysisTemplates
        if (!await context.GradeAnalysisTemplates.AnyAsync())
        {
            context.GradeAnalysisTemplates.AddRange(
                new GradeAnalysisTemplate
                {
                    Name = "Xuất sắc",
                    MinGPA = 8.5m,
                    MaxGPA = 10m,
                    TemplateText = "Sinh viên đạt kết quả xuất sắc. Hãy ghi nhận thành tích và duy trì phong độ. Khuyến khích tiếp tục nghiên cứu sâu và hỗ trợ bạn học.",
                    IsActive = true
                },
                new GradeAnalysisTemplate
                {
                    Name = "Khá tốt",
                    MinGPA = 5m,
                    MaxGPA = 8.49m,
                    TemplateText = "Sinh viên có kết quả khá. Cần tiếp tục nỗ lực và cải thiện các điểm yếu. Tập trung ôn tập và thực hành thêm.",
                    IsActive = true
                },
                new GradeAnalysisTemplate
                {
                    Name = "Cảnh báo học vụ",
                    MinGPA = 0m,
                    MaxGPA = 4.99m,
                    TemplateText = "CẢNH BÁO: Kết quả học tập chưa đạt yêu cầu. Cần gặp cố vấn học tập ngay. Xem xét đăng ký học lại và tăng cường học tập.",
                    IsActive = true
                }
            );
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password, string email)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(email));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
}
