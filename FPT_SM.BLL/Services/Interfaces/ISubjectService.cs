using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectDto>> GetAllAsync();
    Task<SubjectDto?> GetByIdAsync(int id);
    Task<ServiceResult<SubjectDto>> CreateAsync(CreateSubjectDto dto);
    Task<ServiceResult<SubjectDto>> UpdateAsync(int id, UpdateSubjectDto dto);
    Task<ServiceResult> DeleteAsync(int id);
    Task<bool> HasCircularDependency(int subjectId, int prerequisiteId);
}
