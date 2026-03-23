using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IEnrollmentService
{
    Task<List<EnrollmentDto>> GetByStudentAsync(int studentId, int? semesterId = null);
    Task<List<AvailableSubjectDto>> GetAvailableSubjectsAsync(int studentId, int semesterId);
    Task<ServiceResult> ProcessEnrollmentAsync(int studentId, EnrollRegistrationDto dto);
    Task<ServiceResult> DropEnrollmentAsync(int enrollmentId, int studentId);
    Task<bool> HasPassedPrerequisitesAsync(int studentId, int subjectId);
    Task<List<EnrollmentDto>> GetByClassAsync(int classId);
}
