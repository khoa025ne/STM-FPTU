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

        var hasRoles = false;
        try
        {
            hasRoles = await context.Roles.AnyAsync();
        }
        catch (Exception ex) when (IsMissingTableError(ex))
        {
            try
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }
            catch
            {
                await context.Database.EnsureCreatedAsync();
            }

            hasRoles = await context.Roles.AnyAsync();
        }

        // ==================== SEED ROLES ====================
        if (!hasRoles)
        {
            context.Roles.AddRange(
                new Role { Id = 1, Name = "Admin", Description = "Quản trị viên hệ thống" },
                new Role { Id = 2, Name = "Manager", Description = "Quản lý học vụ" },
                new Role { Id = 3, Name = "Teacher", Description = "Giảng viên" },
                new Role { Id = 4, Name = "Student", Description = "Sinh viên" }
            );
            await context.SaveChangesAsync();
        }

        // ==================== SEED USERS ====================
        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
            {
                // Admin
                new User
                {
                    FullName = "Quản Trị Viên",
                    Email = "admin@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "admin@fpt.edu.vn"),
                    RoleId = 1,
                    WalletBalance = 0,
                    IsActive = true
                },
                // Manager
                new User
                {
                    FullName = "Nguyễn Văn Manager",
                    Email = "manager@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "manager@fpt.edu.vn"),
                    RoleId = 2,
                    WalletBalance = 0,
                    IsActive = true
                },
                // Teachers
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
                    FullName = "Phạm Văn Quân",
                    Email = "teacher02@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "teacher02@fpt.edu.vn"),
                    RoleId = 3,
                    WalletBalance = 0,
                    IsActive = true
                },
                new User
                {
                    FullName = "Lý Thị Thảo",
                    Email = "teacher03@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "teacher03@fpt.edu.vn"),
                    RoleId = 3,
                    WalletBalance = 0,
                    IsActive = true
                },
                // Students
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
                },
                new User
                {
                    RollNumber = "SE100003",
                    FullName = "Nguyễn Hữu Tuấn",
                    Email = "student03@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student03@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 15000000,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100004",
                    FullName = "Trần Thị Hương",
                    Email = "student04@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student04@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 12000000,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100005",
                    FullName = "Vũ Quốc Anh",
                    Email = "student05@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student05@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 18000000,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100006",
                    FullName = "Đỗ Thu Trang",
                    Email = "student06@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student06@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 10000000,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100007",
                    FullName = "Hoàng Minh Hiệu",
                    Email = "student07@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student07@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 25000000,
                    IsActive = true
                },
                new User
                {
                    RollNumber = "SE100008",
                    FullName = "Bùi Thị Lan",
                    Email = "student08@fpt.edu.vn",
                    PasswordHash = HashPassword("123", "student08@fpt.edu.vn"),
                    RoleId = 4,
                    WalletBalance = 8000000,
                    IsActive = true
                }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        // ==================== SEED SEMESTERS ====================
        if (!await context.Semesters.AnyAsync())
        {
            context.Semesters.AddRange(
                new Semester
                {
                    Name = "Spring 2024",
                    Code = "SP24",
                    StartDate = new DateTime(2024, 1, 8),
                    EndDate = new DateTime(2024, 4, 30),
                    Status = 3 // Finished
                },
                new Semester
                {
                    Name = "Fall 2024",
                    Code = "FA24",
                    StartDate = new DateTime(2024, 9, 2),
                    EndDate = new DateTime(2025, 1, 15),
                    Status = 3 // Finished
                },
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
                    EndDate = new DateTime(2026, 1, 30),
                    Status = 2 // Ongoing
                },
                new Semester
                {
                    Name = "Spring 2026",
                    Code = "SP26",
                    StartDate = new DateTime(2026, 1, 12),
                    EndDate = new DateTime(2026, 5, 30),
                    Status = 1 // Upcoming
                }
            );
            await context.SaveChangesAsync();
        }

        // ==================== SEED SUBJECTS ====================
        if (!await context.Subjects.AnyAsync())
        {
            context.Subjects.AddRange(
                // Program semester 1
                new Subject { Code = "PRX101", Name = "Lập trình C cơ bản", Credits = 3, Price = 6000000, Description = "Môn học nhập môn lập trình với ngôn ngữ C", ProgramSemester = 1 },
                new Subject { Code = "MAE101", Name = "Toán cho Kỹ thuật", Credits = 3, Price = 6000000, Description = "Toán ứng dụng trong kỹ thuật phần mềm", ProgramSemester = 1 },
                new Subject { Code = "CSI101", Name = "Kỹ năng CNTT cơ bản", Credits = 3, Price = 5500000, Description = "Nền tảng kỹ năng công nghệ thông tin", ProgramSemester = 1 },
                new Subject { Code = "SSC101", Name = "Kỹ năng mềm cho sinh viên", Credits = 2, Price = 4500000, Description = "Giao tiếp và làm việc nhóm", ProgramSemester = 1 },
                new Subject { Code = "ENG101", Name = "Tiếng Anh học thuật 1", Credits = 3, Price = 5000000, Description = "Tiếng Anh cơ bản cho đại học", ProgramSemester = 1 },

                // Program semester 2
                new Subject { Code = "PRO102", Name = "Lập trình nâng cao", Credits = 3, Price = 6000000, Description = "Các kỹ thuật lập trình nâng cao", ProgramSemester = 2 },
                new Subject { Code = "DBI202", Name = "Nhập môn Cơ sở dữ liệu", Credits = 3, Price = 6000000, Description = "Thiết kế và quản lý cơ sở dữ liệu", ProgramSemester = 2 },
                new Subject { Code = "OSG202", Name = "Nguyên lý hệ điều hành", Credits = 3, Price = 6200000, Description = "Kiến trúc và cơ chế hệ điều hành", ProgramSemester = 2 },
                new Subject { Code = "MAD202", Name = "Toán rời rạc", Credits = 3, Price = 5800000, Description = "Nền tảng toán cho khoa học máy tính", ProgramSemester = 2 },
                new Subject { Code = "ENG102", Name = "Tiếng Anh học thuật 2", Credits = 3, Price = 5000000, Description = "Tiếng Anh học thuật nâng cao", ProgramSemester = 2 },

                // Program semester 3
                new Subject { Code = "PRO192", Name = "Lập trình OOP với Java", Credits = 3, Price = 6000000, Description = "Lập trình hướng đối tượng với Java", ProgramSemester = 3 },
                new Subject { Code = "WED201", Name = "Lập trình Web cơ bản", Credits = 3, Price = 6000000, Description = "HTML, CSS, JavaScript cơ bản", ProgramSemester = 3 },
                new Subject { Code = "DSA301", Name = "Cấu trúc dữ liệu và giải thuật", Credits = 3, Price = 6500000, Description = "Thuật toán và cấu trúc dữ liệu", ProgramSemester = 3 },
                new Subject { Code = "PRM301", Name = "Lập trình .NET cơ bản", Credits = 3, Price = 6200000, Description = "Nền tảng phát triển ứng dụng .NET", ProgramSemester = 3 },
                new Subject { Code = "NWC301", Name = "Mạng máy tính cơ bản", Credits = 3, Price = 6000000, Description = "Nền tảng truyền thông mạng", ProgramSemester = 3 },

                // Program semester 4
                new Subject { Code = "WED202", Name = "Lập trình Web nâng cao", Credits = 3, Price = 7000000, Description = "ASP.NET Core, Razor Pages", ProgramSemester = 4 },
                new Subject { Code = "NET201", Name = "Mạng máy tính", Credits = 3, Price = 6000000, Description = "Cơ bản về mạng máy tính", ProgramSemester = 4 },
                new Subject { Code = "SWT401", Name = "Kiểm thử phần mềm", Credits = 3, Price = 6500000, Description = "Kỹ thuật và quy trình kiểm thử", ProgramSemester = 4 },
                new Subject { Code = "PRN401", Name = "Lập trình ứng dụng nâng cao", Credits = 3, Price = 6800000, Description = "Xây dựng ứng dụng thực tế", ProgramSemester = 4 },
                new Subject { Code = "DBI302", Name = "Quản trị cơ sở dữ liệu", Credits = 3, Price = 6500000, Description = "Tối ưu và vận hành database", ProgramSemester = 4 },

                // Program semester 5
                new Subject { Code = "MOB201", Name = "Phát triển ứng dụng di động", Credits = 3, Price = 7000000, Description = "Lập trình Android và iOS", ProgramSemester = 5 },
                new Subject { Code = "SWA201", Name = "Kiến trúc phần mềm", Credits = 3, Price = 6500000, Description = "Các mẫu thiết kế và kiến trúc", ProgramSemester = 5 },
                new Subject { Code = "MLN501", Name = "Nhập môn máy học", Credits = 3, Price = 7200000, Description = "Các kỹ thuật machine learning cơ bản", ProgramSemester = 5 },
                new Subject { Code = "CLD501", Name = "Điện toán đám mây", Credits = 3, Price = 7000000, Description = "Triển khai và vận hành trên cloud", ProgramSemester = 5 },
                new Subject { Code = "CPS501", Name = "Đồ án chuyên ngành", Credits = 4, Price = 8000000, Description = "Dự án tích hợp kiến thức chuyên ngành", ProgramSemester = 5 }
            );
            await context.SaveChangesAsync();

            // Seed Prerequisites
            var prerequisites = new List<(string subjectCode, string preCode)>
            {
                ("PRO102", "PRX101"),
                ("PRO192", "PRX101"),
                ("DSA301", "PRO102"),
                ("PRM301", "PRO192"),
                ("WED202", "WED201"),
                ("PRN401", "PRO192"),
                ("DBI302", "DBI202"),
                ("MOB201", "PRO192"),
                ("SWA201", "PRO192"),
                ("MLN501", "DSA301"),
                ("CLD501", "PRN401"),
                ("CPS501", "SWA201")
            };

            foreach (var (subject, prereq) in prerequisites)
            {
                var subjectEntity = await context.Subjects.FirstAsync(s => s.Code == subject);
                var prereqEntity = await context.Subjects.FirstAsync(s => s.Code == prereq);
                if (!await context.Prerequisites.AnyAsync(p => p.SubjectId == subjectEntity.Id))
                {
                    context.Prerequisites.Add(new Prerequisite
                    {
                        SubjectId = subjectEntity.Id,
                        PrerequisiteSubjectId = prereqEntity.Id
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        // ==================== SEED SLOTS ====================
        if (!await context.Slots.AnyAsync())
        {
            context.Slots.AddRange(
                new Slot { Name = "Slot 1 - Thứ 2 sáng", DayOfWeek = 2, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 30, 0) },
                new Slot { Name = "Slot 2 - Thứ 2 chiều", DayOfWeek = 2, StartTime = new TimeSpan(9, 45, 0), EndTime = new TimeSpan(11, 45, 0) },
                new Slot { Name = "Slot 3 - Thứ 3 sáng", DayOfWeek = 3, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 30, 0) },
                new Slot { Name = "Slot 4 - Thứ 3 chiều", DayOfWeek = 3, StartTime = new TimeSpan(9, 45, 0), EndTime = new TimeSpan(11, 45, 0) },
                new Slot { Name = "Slot 5 - Thứ 4 sáng", DayOfWeek = 4, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 30, 0) },
                new Slot { Name = "Slot 6 - Thứ 4 chiều", DayOfWeek = 4, StartTime = new TimeSpan(9, 45, 0), EndTime = new TimeSpan(11, 45, 0) },
                new Slot { Name = "Slot 7 - Thứ 5 sáng", DayOfWeek = 5, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 30, 0) },
                new Slot { Name = "Slot 8 - Thứ 5 chiều", DayOfWeek = 5, StartTime = new TimeSpan(9, 45, 0), EndTime = new TimeSpan(11, 45, 0) }
            );
            await context.SaveChangesAsync();
        }

        // ==================== SEED CLASSES ====================
        if (!await context.Classes.AnyAsync())
        {
            var fa25 = await context.Semesters.FirstAsync(s => s.Code == "FA25");
            var teachers = await context.Users.Where(u => u.RoleId == 3).ToListAsync();
            var subjects = await context.Subjects.OrderBy(s => s.ProgramSemester).ThenBy(s => s.Code).ToListAsync();
            var slots = await context.Slots.ToListAsync();

            var classes = new List<Class>();
            for (int i = 0; i < subjects.Count; i++)
            {
                var subject = subjects[i];
                for (int section = 1; section <= 2; section++)
                {
                    var teacher = teachers[(i + section - 1) % teachers.Count];
                    var slot = slots[(i * 2 + section - 1) % slots.Count];
                    var capacity = section == 1 ? 30 : 25;
                    var enrolled = Math.Min(capacity - 2, 2 + ((i + section) % 5));

                    classes.Add(new Class
                    {
                        Name = $"{subject.Code}_C{section:00}",
                        SubjectId = subject.Id,
                        TeacherId = teacher.Id,
                        SemesterId = fa25.Id,
                        SlotId = slot.Id,
                        Capacity = capacity,
                        Enrolled = enrolled,
                        Status = 2,
                        RoomNumber = $"B{2 + (i % 3)}.{(i + section):00}"
                    });
                }
            }

            context.Classes.AddRange(classes);
            await context.SaveChangesAsync();
        }

        // ==================== SEED ENROLLMENTS + GRADES + ATTENDANCES ====================
        if (!await context.Enrollments.AnyAsync())
        {
            var fa25 = await context.Semesters.FirstAsync(s => s.Code == "FA25");
            var students = await context.Users.Where(u => u.RoleId == 4).ToListAsync();
            var classes = await context.Classes.ToListAsync();

            var enrollmentList = new List<Enrollment>();
            var gradeList = new List<Grade>();
            // Attendance records are NOT seeded - teacher must mark manually for testing


            // Enrollment Matrix
            var enrollmentMatrix = new Dictionary<int, List<int>>
            {
                { 0, new List<int> { 0, 2, 4, 5 } },  // student01 -> classes 0,2,4,5
                { 1, new List<int> { 0, 1, 3 } },     // student02 -> classes 0,1,3
                { 2, new List<int> { 1, 2, 5 } },     // student03 -> classes 1,2,5
                { 3, new List<int> { 0, 3, 6 } },     // student04 -> classes 0,3,6
                { 4, new List<int> { 2, 4, 7 } },     // student05 -> classes 2,4,7
                { 5, new List<int> { 1, 5 } },        // student06 -> classes 1,5
                { 6, new List<int> { 3, 4, 6 } },     // student07 -> classes 3,4,6
                { 7, new List<int> { 0, 2, 7 } }      // student08 -> classes 0,2,7
            };

            foreach (var studentIdx in enrollmentMatrix.Keys)
            {
                foreach (var classIdx in enrollmentMatrix[studentIdx])
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = students[studentIdx].Id,
                        ClassId = classes[classIdx].Id,
                        SemesterId = fa25.Id,
                        Status = 2,
                        EnrolledAt = DateTime.Now.AddMonths(-2)
                    };
                    enrollmentList.Add(enrollment);
                }
            }

            await context.Enrollments.AddRangeAsync(enrollmentList);
            await context.SaveChangesAsync();

            // Fetch enrollments to add grades and attendances
            var enrollments = await context.Enrollments.ToListAsync();

            // Add Grades
            var random = new Random(123);
            foreach (var enrollment in enrollments)
            {
                var midterm = Math.Round((decimal)(random.Next(5, 10) + random.NextDouble()), 1);
                var final = Math.Round((decimal)(random.Next(4, 10) + random.NextDouble()), 1);
                var gpa = Math.Round((midterm * 0.3m + final * 0.7m), 1);

                var grade = new Grade
                {
                    EnrollmentId = enrollment.Id,
                    MidtermScore = midterm,
                    FinalScore = final,
                    GPA = gpa,
                    IsPassed = gpa >= 4,
                    Status = 2,
                    PublishedAt = DateTime.Now.AddMonths(-1)
                };
                gradeList.Add(grade);

                // NOTE: Attendance records are NOT seeded here
                // Teacher must manually create attendance records via the attendance marking page
                // This allows testing the attendance creation logic
            }

            await context.Grades.AddRangeAsync(gradeList);
            // NOT seeding attendance - teacher will mark manually
            // await context.Attendances.AddRangeAsync(attendanceList);
            await context.SaveChangesAsync();
        }

        // ==================== SEED SPRING 2026 CLASSES ====================
        var sp26ClassExists = await context.Classes.AnyAsync(c => c.Semester.Code == "SP26");
        if (!sp26ClassExists)
        {
            var sp26 = await context.Semesters.FirstAsync(s => s.Code == "SP26");
            var teachers = await context.Users.Where(u => u.RoleId == 3).ToListAsync();
            var subjects = await context.Subjects.OrderBy(s => s.ProgramSemester).ThenBy(s => s.Code).Take(5).ToListAsync();
            var slots = await context.Slots.ToListAsync();

            var classes = new List<Class>();
            for (int i = 0; i < subjects.Count; i++)
            {
                var subject = subjects[i];
                for (int section = 1; section <= 2; section++)
                {
                    var teacher = teachers[(i + section - 1) % teachers.Count];
                    var slot = slots[(i * 2 + section - 1) % slots.Count];
                    var capacity = section == 1 ? 30 : 25;
                    var enrolled = Math.Min(capacity - 2, 2 + ((i + section) % 5));

                    classes.Add(new Class
                    {
                        Name = $"{subject.Code}_C{section:00}",
                        SubjectId = subject.Id,
                        TeacherId = teacher.Id,
                        SemesterId = sp26.Id,
                        SlotId = slot.Id,
                        Capacity = capacity,
                        Enrolled = enrolled,
                        Status = 1, // Upcoming
                        RoomNumber = $"B{2 + (i % 3)}.{(i + section):00}"
                    });
                }
            }

            context.Classes.AddRange(classes);
            await context.SaveChangesAsync();
        }

        // ==================== SEED SPRING 2026 ENROLLMENTS ====================
        var sp26EnrollmentExists = await context.Enrollments.AnyAsync(e => e.Semester.Code == "SP26");
        if (!sp26EnrollmentExists)
        {
            var sp26 = await context.Semesters.FirstAsync(s => s.Code == "SP26");
            var students = await context.Users.Where(u => u.RoleId == 4).ToListAsync();
            var sp26Classes = await context.Classes
                .Include(c => c.Semester)
                .Where(c => c.Semester.Code == "SP26")
                .OrderBy(c => c.Id)
                .ToListAsync();

            var enrollmentList = new List<Enrollment>();

            // Enrollment Matrix for Spring 2026 - 10 classes total (5 subjects × 2 classes each)
            var enrollmentMatrix = new Dictionary<int, List<int>>
            {
                { 0, new List<int> { 0, 2, 4, 6 } },     // student01 -> classes 0,2,4,6
                { 1, new List<int> { 0, 1, 3, 5 } },     // student02 -> classes 0,1,3,5
                { 2, new List<int> { 1, 2, 5, 7 } },     // student03 -> classes 1,2,5,7
                { 3, new List<int> { 3, 6, 8 } },        // student04 -> classes 3,6,8
                { 4, new List<int> { 2, 4, 7, 9 } },     // student05 -> classes 2,4,7,9
                { 5, new List<int> { 1, 5, 8 } },        // student06 -> classes 1,5,8
                { 6, new List<int> { 3, 4, 6, 9 } },     // student07 -> classes 3,4,6,9
                { 7, new List<int> { 0, 2, 8, 9 } }      // student08 -> classes 0,2,8,9
            };

            foreach (var studentIdx in enrollmentMatrix.Keys)
            {
                foreach (var classIdx in enrollmentMatrix[studentIdx])
                {
                    if (classIdx < sp26Classes.Count)
                    {
                        var enrollment = new Enrollment
                        {
                            StudentId = students[studentIdx].Id,
                            ClassId = sp26Classes[classIdx].Id,
                            SemesterId = sp26.Id,
                            Status = 1, // Upcoming
                            EnrolledAt = DateTime.Now
                        };
                        enrollmentList.Add(enrollment);
                    }
                }
            }

            await context.Enrollments.AddRangeAsync(enrollmentList);
            await context.SaveChangesAsync();
        }

        // ==================== SEED QUIZZES ====================
        if (!await context.Quizzes.AnyAsync())
        {
            var classes = await context.Classes.ToListAsync();
            var quizzes = new List<Quiz>();

            foreach (var cls in classes.Take(5))
            {
                quizzes.Add(new Quiz
                {
                    ClassId = cls.Id,
                    Title = $"Bài kiểm tra giữa kỳ - {cls.Name}",
                    Description = "Bài kiểm tra trắc nghiệm giữa kỳ",
                    StartTime = DateTime.Now.AddDays(3),
                    EndTime = DateTime.Now.AddDays(3).AddHours(1).AddMinutes(30),
                    DurationMinutes = 90,
                    QuizType = "Midterm",
                    IsPublishResult = true,
                    Status = 2
                });

                quizzes.Add(new Quiz
                {
                    ClassId = cls.Id,
                    Title = $"Bài kiểm tra cuối kỳ - {cls.Name}",
                    Description = "Bài kiểm tra trắc nghiệm cuối kỳ",
                    StartTime = DateTime.Now.AddDays(30),
                    EndTime = DateTime.Now.AddDays(30).AddHours(2),
                    DurationMinutes = 120,
                    QuizType = "Final",
                    IsPublishResult = true,
                    Status = 1
                });

                quizzes.Add(new Quiz
                {
                    ClassId = cls.Id,
                    Title = $"Bài luyện tập tập 1 - {cls.Name}",
                    Description = "Bài tập luyện tập định kỳ",
                    StartTime = DateTime.Now.AddDays(-5),
                    EndTime = DateTime.Now.AddDays(-5).AddMinutes(45),
                    DurationMinutes = 45,
                    QuizType = "Practice",
                    IsPublishResult = true,
                    Status = 2
                });
            }

            context.Quizzes.AddRange(quizzes);
            await context.SaveChangesAsync();
        }

        // ==================== SEED QUIZ QUESTIONS ====================
        if (!await context.QuizQuestions.AnyAsync())
        {
            var quizzes = await context.Quizzes.ToListAsync();
            var questions = new List<QuizQuestion>();

            var questionTexts = new List<string>
            {
                "Biến trong C được khai báo như thế nào?",
                "Vòng lặp for trong Java có cú pháp nào?",
                "Lớp trong OOP là gì?",
                "SQL SELECT dùng để làm gì?",
                "HTML là viết tắt của từ gì?",
                "CSS dùng để điều khiển gì?",
                "Cơ sở dữ liệu quan hệ là gì?",
                "Biến static trong Java có chức năng gì?",
                "Danh sách được sắp xếp từ nhỏ đến lớn gọi là gì?",
                "Hàm trong Java được định nghĩa như thế nào?"
            };

            foreach (var quiz in quizzes)
            {
                for (int i = 0; i < 5; i++)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.Id,
                        QuestionText = questionTexts[(i + quiz.Id) % questionTexts.Count],
                        QuestionType = "SingleChoice",
                        Points = 2,
                        OrderIndex = i + 1
                    };
                    questions.Add(question);
                }
            }

            context.QuizQuestions.AddRange(questions);
            await context.SaveChangesAsync();
        }

        // ==================== SEED QUIZ ANSWER OPTIONS ====================
        if (!await context.QuizAnswerOptions.AnyAsync())
        {
            var questions = await context.QuizQuestions.ToListAsync();
            var options = new List<QuizAnswerOption>();

            var answerTexts = new List<string>[]
            {
                new List<string> { "int a;", "int a = ;", "a int;", "int a ;" },
                new List<string> { "for(int i=0; i<10; i++)", "for i=0 to 10", "for(i in range)", "foreach i" },
                new List<string> { "Một mẫu để tạo đối tượng", "Một biến", "Một hàm", "Một file" },
                new List<string> { "Lấy dữ liệu từ bảng", "Thêm dữ liệu", "Xóa dữ liệu", "Cập nhật dữ liệu" },
                new List<string> { "HyperText Markup Language", "Home Tool Markup Language", "High Tech Markup Language", "Hyper Text Markup Library" },
                new List<string> { "Kiểu dữ liệu", "Giao diện, bố cục", "Cơ sở dữ liệu", "Logic chương trình" },
                new List<string> { "Cơ sở dữ liệu sử dụng bảng", "Cơ sở dữ liệu không có cấu trúc", "Cơ sở dữ liệu đơn giản", "Cơ sở dữ liệu tế bào" },
                new List<string> { "Biến được chia sẻ trong tất cả đối tượng", "Biến cục bộ", "Biến tạm thời", "Không biến gì" },
                new List<string> { "Sắp xếp tăng dần", "Sắp xếp giảm dần", "Sắp xếp ngẫu nhiên", "Sắp xếp chữ cái" },
                new List<string> { "returnType name() {}", "void name() {}", "function name()", "def name():" }
            };

            var correctAnswers = new List<int> { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 };

            foreach (var question in questions)
            {
                var answerIndex = (question.Id - 1) % answerTexts.Length;
                var answerSet = answerTexts[answerIndex];
                var correctIndex = correctAnswers[answerIndex];

                for (int i = 0; i < answerSet.Count; i++)
                {
                    options.Add(new QuizAnswerOption
                    {
                        QuestionId = question.Id,
                        OptionText = answerSet[i],
                        IsCorrect = i == correctIndex,
                        OrderIndex = i + 1
                    });
                }
            }

            context.QuizAnswerOptions.AddRange(options);
            await context.SaveChangesAsync();
        }

        // ==================== SEED QUIZ SUBMISSIONS ====================
        if (!await context.QuizSubmissions.AnyAsync())
        {
            var quizzes = await context.Quizzes.Where(q => q.Status == 2).Take(3).ToListAsync();
            var students = await context.Users.Where(u => u.RoleId == 4).ToListAsync();
            var submissions = new List<QuizSubmission>();

            foreach (var quiz in quizzes)
            {
                foreach (var student in students.Take(4))
                {
                    var submission = new QuizSubmission
                    {
                        QuizId = quiz.Id,
                        StudentId = student.Id,
                        StartedAt = quiz.StartTime.AddMinutes(-5),
                        SubmittedAt = quiz.StartTime.AddMinutes(60),
                        TotalScore = Math.Round((decimal)(new Random().Next(6, 10)), 1),
                        Status = "Graded"
                    };
                    submissions.Add(submission);
                }
            }

            context.QuizSubmissions.AddRange(submissions);
            await context.SaveChangesAsync();
        }

        // ==================== SEED QUIZ STUDENT ANSWERS ====================
        if (!await context.QuizStudentAnswers.AnyAsync())
        {
            var submissions = await context.QuizSubmissions.ToListAsync();
            var studentAnswers = new List<QuizStudentAnswer>();
            var random = new Random(456);

            foreach (var submission in submissions)
            {
                var questions = await context.QuizQuestions.Where(q => q.QuizId == submission.QuizId).ToListAsync();
                foreach (var question in questions)
                {
                    var options = await context.QuizAnswerOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
                    var correctOption = options.FirstOrDefault(o => o.IsCorrect);
                    var selectedOption = options[random.Next(options.Count)];

                    studentAnswers.Add(new QuizStudentAnswer
                    {
                        SubmissionId = submission.Id,
                        QuestionId = question.Id,
                        SelectedOptionIds = selectedOption.Id.ToString(),
                        IsCorrect = selectedOption.Id == correctOption?.Id,
                        ScoreEarned = selectedOption.Id == correctOption?.Id ? question.Points : 0
                    });
                }
            }

            context.QuizStudentAnswers.AddRange(studentAnswers);
            await context.SaveChangesAsync();
        }

        // ==================== SEED TRANSACTIONS ====================
        if (!await context.Transactions.AnyAsync())
        {
            var students = await context.Users.Where(u => u.RoleId == 4).ToListAsync();
            var transactions = new List<Transaction>();
            var random = new Random(789);

            // Add guaranteed base transactions for each student (starting balance)
            foreach (var student in students)
            {
                var now = DateTime.Now;
                // Add 5 guaranteed deposits for each student in current month
                for (int i = 0; i < 5; i++)
                {
                    transactions.Add(new Transaction
                    {
                        UserId = student.Id,
                        Amount = 2000000 + (i * 500000),  // 2M, 2.5M, 3M, 3.5M, 4M
                        Type = "Deposit",
                        Status = "Success",
                        Description = "Nạp tiền vào ví",
                        VnPayTransactionId = $"VNP{now.Ticks}{i:D4}",
                        CreatedAt = now.AddDays(-(5 - i))
                    });
                }

                // Add 2 guaranteed tuition payments for balance
                for (int i = 0; i < 2; i++)
                {
                    transactions.Add(new Transaction
                    {
                        UserId = student.Id,
                        Amount = 2500000m,
                        Type = "TuitionPayment",
                        Status = "Success",
                        Description = "Thanh toán học phí",
                        OrderId = $"ORD{now.Ticks}{i:D4}",
                        CreatedAt = now.AddDays(-(3 - i))
                    });
                }
            }

            // Add transactions for 2021-2026 across all months for comprehensive demo data
            var yearMonthRanges = new List<(int year, int month)>();
            
            // 2021: Jan-Dec
            for (int m = 1; m <= 12; m++)
                yearMonthRanges.Add((2021, m));
            
            // 2022: Jan-Dec
            for (int m = 1; m <= 12; m++)
                yearMonthRanges.Add((2022, m));
            
            // 2023: Jan-Dec
            for (int m = 1; m <= 12; m++)
                yearMonthRanges.Add((2023, m));
            
            // 2024: Jan-Dec
            for (int m = 1; m <= 12; m++)
                yearMonthRanges.Add((2024, m));
            
            // 2025: Jan-Dec
            for (int m = 1; m <= 12; m++)
                yearMonthRanges.Add((2025, m));
            
            // 2026: Jan-Mar (current year up to now)
            for (int m = 1; m <= 3; m++)
                yearMonthRanges.Add((2026, m));

            foreach (var student in students)
            {
                foreach (var (year, month) in yearMonthRanges)
                {
                    // Create date for this month (random day)
                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    var day = random.Next(1, daysInMonth + 1);
                    var transactionDate = new DateTime(year, month, day);

                    // Skip if date is in future
                    if (transactionDate > DateTime.Now)
                        continue;

                    // Generate 3-5 deposits per month for more revenue data
                    int depositCount = random.Next(3, 6);
                    for (int d = 0; d < depositCount; d++)
                    {
                        var depositDay = random.Next(1, Math.Min(28, daysInMonth) + 1);
                        var depositDate = new DateTime(year, month, depositDay);
                        
                        if (depositDate <= DateTime.Now)
                        {
                            transactions.Add(new Transaction
                            {
                                UserId = student.Id,
                                Amount = random.Next(5000000, 15000000),
                                Type = "Deposit",
                                Status = "Success",
                                Description = "Nạp tiền vào ví",
                                VnPayTransactionId = $"VNP{year}{month:D2}{depositDay:D2}{random.Next(10000, 99999)}",
                                CreatedAt = depositDate
                            });
                        }
                    }

                    // Tuition payment (65% chance, 2-3 times per month)
                    if (random.Next(0, 100) < 65)
                    {
                        int paymentCount = random.Next(2, 4);
                        var subjects = new[] { "PRX101", "PRO102", "DBI202", "WED201", "SWA201", "DSA301", "WED202", "NET201", "MOB201" };
                        
                        for (int p = 0; p < paymentCount; p++)
                        {
                            var paymentDay = random.Next(1, Math.Min(28, daysInMonth) + 1);
                            var paymentDate = new DateTime(year, month, paymentDay);
                            
                            if (paymentDate <= DateTime.Now)
                            {
                                transactions.Add(new Transaction
                                {
                                    UserId = student.Id,
                                    Amount = random.Next(3000000, 8000000),
                                    Type = "TuitionPayment",
                                    Status = "Success",
                                    Description = $"Thanh toán học phí {subjects[random.Next(subjects.Length)]}",
                                    OrderId = $"ORD{year}{month:D2}{paymentDay:D2}{random.Next(10000, 99999)}",
                                    CreatedAt = paymentDate
                                });
                            }
                        }
                    }

                    // Refund transaction (20% chance)
                    if (random.Next(0, 100) < 20)
                    {
                        var refundDay = random.Next(1, Math.Min(28, daysInMonth) + 1);
                        var refundDate = new DateTime(year, month, refundDay);
                        
                        if (refundDate <= DateTime.Now)
                        {
                            transactions.Add(new Transaction
                            {
                                UserId = student.Id,
                                Amount = random.Next(1000000, 3000000),
                                Type = "Refund",
                                Status = "Success",
                                Description = "Hoàn tiền hủy đăng ký môn học",
                                CreatedAt = refundDate
                            });
                        }
                    }
                }

                // Add extensive recent transactions for current period (10-15 transactions for current month)
                var March2026 = new DateTime(2026, 3, 1);
                for (int i = 0; i < 10; i++)
                {
                    var dayOffset = random.Next(1, 26);
                    transactions.Add(new Transaction
                    {
                        UserId = student.Id,
                        Amount = random.Next(5000000, 8000000),
                        Type = "Deposit",
                        Status = "Success",
                        Description = "Nạp tiền vào ví",
                        VnPayTransactionId = $"VNP{DateTime.Now.Ticks}{i:D4}",
                        CreatedAt = March2026.AddDays(dayOffset)
                    });
                }
                
                // Add current month tuition payments
                for (int i = 0; i < 3; i++)
                {
                    var dayOffset = random.Next(1, 26);
                    var subjects = new[] { "PRX101", "PRO102", "DBI202", "WED201", "SWA201", "DSA301" };
                    transactions.Add(new Transaction
                    {
                        UserId = student.Id,
                        Amount = random.Next(3000000, 6000000),
                        Type = "TuitionPayment",
                        Status = "Success",
                        Description = $"Thanh toán học phí {subjects[random.Next(subjects.Length)]}",
                        OrderId = $"ORD{DateTime.Now.Ticks}{i:D4}",
                        CreatedAt = March2026.AddDays(dayOffset)
                    });
                }
            }

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();

            // ==================== RECALCULATE WALLET BALANCES ====================
            // Update each student's wallet balance based on their transactions
            var studentUsers = await context.Users.Where(u => u.RoleId == 4).ToListAsync();
            foreach (var student in studentUsers)
            {
                var studentTransactions = await context.Transactions.Where(t => t.UserId == student.Id).ToListAsync();
                decimal newBalance = 0;

                foreach (var txn in studentTransactions)
                {
                    if (txn.Type == "Deposit" && txn.Status == "Success")
                    {
                        newBalance += txn.Amount;
                    }
                    else if (txn.Type == "Refund" && txn.Status == "Success")
                    {
                        newBalance += txn.Amount;
                    }
                    else if (txn.Type == "TuitionPayment" && txn.Status == "Success")
                    {
                        newBalance -= txn.Amount;
                    }
                }

                student.WalletBalance = Math.Max(0, newBalance); // Ensure balance doesn't go negative
                context.Users.Update(student);
            }

            await context.SaveChangesAsync();
        }

        // ==================== SEED CHAT SESSIONS & CHAT LOGS ====================
        if (!await context.ChatSessions.AnyAsync())
        {
            var users = await context.Users.Where(u => u.RoleId == 4).ToListAsync();
            var chatSessions = new List<ChatSession>();
            var chatLogs = new List<ChatLog>();

            foreach (var user in users.Take(4))
            {
                var session1 = new ChatSession
                {
                    UserId = user.Id,
                    Title = "Hỏi về bài kiểm tra",
                    Topic = "Học tập",
                    IsArchived = false,
                    CreatedAt = DateTime.Now.AddDays(-5)
                };
                chatSessions.Add(session1);

                var session2 = new ChatSession
                {
                    UserId = user.Id,
                    Title = "Tìm hiểu về Java",
                    Topic = "Lập trình",
                    IsArchived = false,
                    CreatedAt = DateTime.Now.AddDays(-10)
                };
                chatSessions.Add(session2);
            }

            await context.ChatSessions.AddRangeAsync(chatSessions);
            await context.SaveChangesAsync();

            var sessions = await context.ChatSessions.ToListAsync();
            foreach (var session in sessions)
            {
                chatLogs.Add(new ChatLog
                {
                    UserId = session.UserId,
                    SessionId = session.Id,
                    Message = "Java là ngôn ngữ gì?",
                    Response = "Java là ngôn ngữ lập trình hướng đối tượng được phát triển bởi Sun Microsystems.",
                    CreatedAt = DateTime.Now.AddDays(-3)
                });

                chatLogs.Add(new ChatLog
                {
                    UserId = session.UserId,
                    SessionId = session.Id,
                    Message = "Làm thế nào để viết class trong Java?",
                    Response = "Class trong Java được khai báo với từ khóa 'class' theo sau bởi tên class và dấu {}",
                    CreatedAt = DateTime.Now.AddDays(-2)
                });
            }

            context.ChatLogs.AddRange(chatLogs);
            await context.SaveChangesAsync();
        }

        // ==================== SEED ACADEMIC ANALYSIS ====================
        if (!await context.AcademicAnalyses.AnyAsync())
        {
            var enrollments = await context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Semester)
                .Include(e => e.Grade)
                .Take(10)
                .ToListAsync();

            var teachers = await context.Users.Where(u => u.RoleId == 3).ToListAsync();
            var analyses = new List<AcademicAnalysis>();

            foreach (var enrollment in enrollments)
            {
                if (enrollment.Grade != null)
                {
                    var analysisText = enrollment.Grade.GPA >= 8 
                        ? "Sinh viên có kết quả xuất sắc, tiếp tục duy trì phong độ."
                        : enrollment.Grade.GPA >= 5
                        ? "Sinh viên cần cải thiện kết quả học tập, tăng cường ôn luyện."
                        : "Cảnh báo: Sinh viên cần gặp cố vấn học tập để lập kế hoạch cải thiện.";

                    analyses.Add(new AcademicAnalysis
                    {
                        StudentId = enrollment.StudentId,
                        ClassId = enrollment.ClassId,
                        SemesterId = enrollment.SemesterId,
                        GradeId = enrollment.Grade.Id,
                        AnalysisText = analysisText,
                        AnalysisJson = $"{{\"gpa\": {enrollment.Grade.GPA}, \"status\": \"reviewed\"}}",
                        CreatedAt = DateTime.Now.AddDays(-15),
                        TriggeredByTeacherId = teachers[0].Id
                    });
                }
            }

            context.AcademicAnalyses.AddRange(analyses);
            await context.SaveChangesAsync();
        }

        // ==================== SEED GRADE ANALYSIS TEMPLATES ====================
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
                    Name = "Giỏi",
                    MinGPA = 7m,
                    MaxGPA = 8.49m,
                    TemplateText = "Sinh viên có kết quả tốt. Tiếp tục duy trì kiến thức vững chắc và nỗ lực hơn nữa.",
                    IsActive = true
                },
                new GradeAnalysisTemplate
                {
                    Name = "Khá",
                    MinGPA = 5.5m,
                    MaxGPA = 6.99m,
                    TemplateText = "Sinh viên có kết quả khá. Cần tăng cường ôn luyện và thực hành thêm để nâng cao kiến thức.",
                    IsActive = true
                },
                new GradeAnalysisTemplate
                {
                    Name = "Trung bình",
                    MinGPA = 4m,
                    MaxGPA = 5.49m,
                    TemplateText = "Sinh viên có kết quả trung bình. Cần tập trung ôn tập lại các kiến thức cơ bản và làm thêm bài tập.",
                    IsActive = true
                },
                new GradeAnalysisTemplate
                {
                    Name = "Cảnh báo học vụ",
                    MinGPA = 0m,
                    MaxGPA = 3.99m,
                    TemplateText = "CẢNH BÁO: Kết quả học tập chưa đạt yêu cầu. Cần gặp cố vấn học tập ngay lập tức để lập kế hoạch cải thiện và xem xét đăng ký học lại.",
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

    private static bool IsMissingTableError(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("1146", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
