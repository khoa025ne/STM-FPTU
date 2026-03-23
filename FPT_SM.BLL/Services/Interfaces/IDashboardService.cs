using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<ManagerDashboardDto> GetManagerDashboardAsync();
    Task<TeacherDashboardDto> GetTeacherDashboardAsync(int teacherId);
    Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId);
}
