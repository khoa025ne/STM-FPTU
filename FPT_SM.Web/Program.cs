using FPT_SM.BLL.Extensions;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Seeder;
using FPT_SM.Web.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(conn, ServerVersion.AutoDetect(conn)));

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => {
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddSignalR();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapHub<AttendanceHub>("/hubs/attendance");
app.MapHub<GradeHub>("/hubs/grade");
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<QuizHub>("/hubs/quiz");

app.MapControllers();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (app.Environment.IsDevelopment())
    {
        // Reset database to ensure schema matches current entities
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        // In production, only create if doesn't exist
        await db.Database.EnsureCreatedAsync();
    }

    await DbSeeder.SeedAsync(scope.ServiceProvider);

    // ==================== CLEANUP ATTENDANCE DATA ====================
    // 1. Remove attendance records with invalid dates (year < 2000, typically 0001)
    var invalidDateAttendances = await db.Attendances
        .Where(a => a.SlotDate.Year < 2000)
        .ToListAsync();
    
    if (invalidDateAttendances.Count > 0)
    {
        Console.WriteLine($"Xoá {invalidDateAttendances.Count} bản ghi điểm danh có ngày không hợp lệ...");
        db.Attendances.RemoveRange(invalidDateAttendances);
        await db.SaveChangesAsync();
        Console.WriteLine("Xoá xong bản ghi ngày không hợp lệ!");
    }

    // 2. Remove duplicate attendance records (same enrollment + date, keep first/lowest SessionNumber only)
    var allAttendances = await db.Attendances.ToListAsync();
    var duplicates = allAttendances
        .GroupBy(a => new { a.EnrollmentId, a.SlotDate })
        .Where(g => g.Count() > 1)
        .SelectMany(g => g.OrderBy(a => a.SessionNumber).Skip(1))
        .ToList();

    if (duplicates.Count > 0)
    {
        Console.WriteLine($"Xoá {duplicates.Count} bản ghi điểm danh trùng lặp...");
        db.Attendances.RemoveRange(duplicates);
        await db.SaveChangesAsync();
        Console.WriteLine("Xoá xong bản ghi trùng lặp!");
    }
}

app.Run();
