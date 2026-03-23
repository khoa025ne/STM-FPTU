using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IReportService
{
    Task<List<EnrollmentReportDto>> GetEnrollmentReportAsync(ReportFilterDto filter);
    Task<List<GradeReportDto>> GetGradeReportAsync(ReportFilterDto filter);
    Task<RevenueReportDto> GetRevenueReportAsync(ReportFilterDto filter);
    Task<AdminOverviewDto> GetAdminOverviewAsync();
    Task<List<SemesterEnrollmentDto>> GetEnrollmentTrendAsync(ReportFilterDto? filter = null);
    Task<List<ManagerClassStatusDto>> GetClassStatusReportAsync();
    Task<List<ManagerTeacherReportDto>> GetTeacherWorkloadAsync();
    Task<List<ClassExportDto>> GetClassListAsync(ReportFilterDto? filter = null);
    Task<byte[]> ExportGradeReportToExcelAsync(ReportFilterDto filter);
    Task<byte[]> ExportGradeReportToPdfAsync(ReportFilterDto filter);
    Task<byte[]> ExportEnrollmentReportToExcelAsync(ReportFilterDto filter);
    Task<byte[]> ExportClassReportToExcelAsync(ReportFilterDto filter);
}
