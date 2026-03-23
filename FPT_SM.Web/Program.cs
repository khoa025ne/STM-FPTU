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
        // Skip auto-reset to preserve payment transactions and test data
        // await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        // In production, only create if doesn't exist
        await db.Database.EnsureCreatedAsync();
    }

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Urls.Add("http://localhost:5008");
app.Run();
