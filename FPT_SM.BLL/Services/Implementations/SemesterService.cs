using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;

namespace FPT_SM.BLL.Services.Implementations;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _repo;

    public SemesterService(ISemesterRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<SemesterDto>> GetAllAsync()
    {
        var semesters = await _repo.GetOrderedAsync();
        return semesters.Select(MapToDto).ToList();
    }

    public async Task<SemesterDto?> GetByIdAsync(int id)
    {
        var s = await _repo.GetByIdAsync(id);
        return s == null ? null : MapToDto(s);
    }

    public async Task<SemesterDto?> GetOngoingAsync()
    {
        var s = await _repo.GetOngoingSemesterAsync();
        return s == null ? null : MapToDto(s);
    }

    public async Task<ServiceResult<SemesterDto>> CreateAsync(CreateSemesterDto dto)
    {
        if (await _repo.AnyAsync(s => s.Code == dto.Code))
            return ServiceResult<SemesterDto>.Failure($"Mã học kỳ '{dto.Code}' đã tồn tại.");

        if (dto.EndDate <= dto.StartDate)
            return ServiceResult<SemesterDto>.Failure("Ngày kết thúc phải sau ngày bắt đầu.");

        var semester = new Semester
        {
            Name = dto.Name,
            Code = dto.Code,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = 1
        };
        await _repo.AddAsync(semester);
        return ServiceResult<SemesterDto>.Success(MapToDto(semester), "Tạo học kỳ thành công!");
    }

    public async Task<ServiceResult<SemesterDto>> UpdateAsync(int id, UpdateSemesterDto dto)
    {
        var semester = await _repo.GetByIdAsync(id);
        if (semester == null) return ServiceResult<SemesterDto>.Failure("Không tìm thấy học kỳ.");

        if (semester.Status is 2 or 3)
            return ServiceResult<SemesterDto>.Failure("Không thể chỉnh sửa học kỳ đang diễn ra hoặc đã kết thúc.");

        if (await _repo.AnyAsync(s => s.Code == dto.Code && s.Id != id))
            return ServiceResult<SemesterDto>.Failure($"Mã học kỳ '{dto.Code}' đã được sử dụng.");

        semester.Name = dto.Name;
        semester.Code = dto.Code;
        semester.StartDate = dto.StartDate;
        semester.EndDate = dto.EndDate;
        await _repo.UpdateAsync(semester);
        return ServiceResult<SemesterDto>.Success(MapToDto(semester), "Cập nhật học kỳ thành công!");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var semester = await _repo.GetByIdAsync(id);
        if (semester == null) return ServiceResult.Failure("Không tìm thấy học kỳ.");
        if (semester.Status != 1) return ServiceResult.Failure("Chỉ có thể xóa học kỳ đang ở trạng thái 'Sắp diễn ra'.");
        if (semester.Classes?.Any() == true) return ServiceResult.Failure("Không thể xóa học kỳ đã có lớp học.");

        // Soft delete
        semester.DeletedAt = DateTime.Now;
        await _repo.UpdateAsync(semester);
        return ServiceResult.Success("Đã xóa học kỳ.");
    }

    public async Task<ServiceResult<SemesterDto>> AdvanceStatusAsync(int id)
    {
        var semester = await _repo.GetByIdAsync(id);
        if (semester == null) return ServiceResult<SemesterDto>.Failure("Không tìm thấy học kỳ.");

        if (semester.Status == 1) // Upcoming → Ongoing
        {
            if (await _repo.AnyAsync(s => s.Status == 2 && s.Id != id))
                return ServiceResult<SemesterDto>.Failure("Đã có một học kỳ đang diễn ra. Không thể kích hoạt thêm.");
            semester.Status = 2;
        }
        else if (semester.Status == 2) // Ongoing → Finished
        {
            semester.Status = 3;
        }
        else if (semester.Status == 3) // Finished → Closed
        {
            semester.Status = 0;
        }
        else
        {
            return ServiceResult<SemesterDto>.Failure("Không thể thay đổi trạng thái học kỳ này.");
        }

        await _repo.UpdateAsync(semester);
        return ServiceResult<SemesterDto>.Success(MapToDto(semester), "Cập nhật trạng thái học kỳ thành công!");
    }

    private static SemesterDto MapToDto(Semester s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Code = s.Code,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        Status = s.Status,
        ClassCount = s.Classes?.Count ?? 0,
        EnrollmentCount = s.Enrollments?.Count ?? 0
    };
}
