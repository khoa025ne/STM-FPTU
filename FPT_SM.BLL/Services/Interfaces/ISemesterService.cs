using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;

namespace FPT_SM.BLL.Services.Interfaces;

public interface ISemesterService
{
    Task<List<SemesterDto>> GetAllAsync();
    Task<SemesterDto?> GetByIdAsync(int id);
    Task<SemesterDto?> GetOngoingAsync();
    Task<ServiceResult<SemesterDto>> CreateAsync(CreateSemesterDto dto);
    Task<ServiceResult<SemesterDto>> UpdateAsync(int id, UpdateSemesterDto dto);
    Task<ServiceResult> DeleteAsync(int id);
    Task<ServiceResult<SemesterDto>> AdvanceStatusAsync(int id);
}
