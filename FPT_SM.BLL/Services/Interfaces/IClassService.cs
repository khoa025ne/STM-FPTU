using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface IClassService
{
    Task<List<ClassDto>> GetAllAsync(int? semesterId = null, int? subjectId = null, int? status = null);
    Task<ClassDto?> GetByIdAsync(int id);
    Task<List<ClassDto>> GetByTeacherAsync(int teacherId, int? semesterId = null);
    Task<List<ClassDto>> GetByTeacherInOngoingSemesterAsync(int teacherId);
    Task<ServiceResult<ClassDto>> CreateAsync(CreateClassDto dto);
    Task<ServiceResult<ClassDto>> UpdateAsync(int id, UpdateClassDto dto);
    Task<ServiceResult> DeleteAsync(int id);
    Task<ServiceResult<ClassDto>> ApproveAsync(int id);
    Task<ServiceResult<ClassDto>> CancelAsync(int id, string? reason = null);
    Task<List<SlotDto>> GetAllSlotsAsync();
    Task<bool> HasScheduleConflictAsync(int semesterId, int slotId, int? excludeClassId = null);
}
