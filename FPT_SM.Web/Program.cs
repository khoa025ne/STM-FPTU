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

var runStartupDbInitialization = builder.Configuration.GetValue<bool>("Database:RunStartupInitialization");
var resetDatabaseOnStartup = builder.Configuration.GetValue<bool>("Database:ResetOnStartup");

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

try
{
    if (runStartupDbInitialization)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));

        var ensureSchema = resetDatabaseOnStartup;

        if (resetDatabaseOnStartup)
        {
            Console.WriteLine("Resetting database on startup...");
            await db.Database.EnsureDeletedAsync();
        }

        if (!ensureSchema)
        {
            try
            {
                _ = await db.Roles.AnyAsync();
            }
            catch (Exception ex) when (
                ex.ToString().Contains("doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                ex.ToString().Contains("1146", StringComparison.OrdinalIgnoreCase))
            {
                ensureSchema = true;
            }
        }

        if (ensureSchema)
        {
            Console.WriteLine("Ensuring database schema...");
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception migrateEx)
            {
                Console.WriteLine($"Migrate failed, fallback to EnsureCreated. Reason: {migrateEx.Message}");
                await db.Database.EnsureCreatedAsync();
            }

            var rolesTableReady = false;
            try
            {
                _ = await db.Roles.AnyAsync();
                rolesTableReady = true;
            }
            catch (Exception ex) when (
                ex.ToString().Contains("doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                ex.ToString().Contains("1146", StringComparison.OrdinalIgnoreCase))
            {
                rolesTableReady = false;
            }

            if (!rolesTableReady)
            {
                Console.WriteLine("Schema is incomplete (missing Roles table). Recreating database...");
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }
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
    else
    {
        Console.WriteLine("Skipping startup database initialization. Set Database:RunStartupInitialization=true to enable.");
    }

    Console.WriteLine("Application initialized successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error during initialization: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");
    throw;
}

app.Run();
