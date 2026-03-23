using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.BLL.Services.Implementations;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepo;
    private readonly ISlotRepository _slotRepo;
    private readonly AppDbContext _context;

    public ClassService(IClassRepository classRepo, ISlotRepository slotRepo, AppDbContext context)
    {
        _classRepo = classRepo;
        _slotRepo = slotRepo;
        _context = context;
    }

    public async Task<List<ClassDto>> GetAllAsync(int? semesterId = null, int? subjectId = null, int? status = null)
    {
        var classes = await _classRepo.GetAllWithDetailsAsync();
        if (semesterId.HasValue) classes = classes.Where(c => c.SemesterId == semesterId.Value);
        if (subjectId.HasValue) classes = classes.Where(c => c.SubjectId == subjectId.Value);
        if (status.HasValue) classes = classes.Where(c => c.Status == status.Value);
        return classes.Select(MapToDto).ToList();
    }

    public async Task<ClassDto?> GetByIdAsync(int id)
    {
        var c = await _classRepo.GetWithDetailsAsync(id);
        return c == null ? null : MapToDto(c);
    }

    public async Task<List<ClassDto>> GetByTeacherAsync(int teacherId, int? semesterId = null)
    {
        var classes = semesterId.HasValue
            ? await _classRepo.GetByTeacherAndSemesterAsync(teacherId, semesterId.Value)
            : await _classRepo.GetByTeacherAsync(teacherId);
        return classes.Select(MapToDto).ToList();
    }

    public async Task<ServiceResult<ClassDto>> CreateAsync(CreateClassDto dto)
    {
        if (await HasScheduleConflictAsync(dto.SemesterId, dto.SlotId))
            return ServiceResult<ClassDto>.Failure("Lịch học bị trùng với lớp khác trong cùng kỳ và ca học này.");

        if (await _classRepo.AnyAsync(c => c.Name == dto.Name && c.SemesterId == dto.SemesterId))
            return ServiceResult<ClassDto>.Failure($"Tên lớp '{dto.Name}' đã tồn tại trong học kỳ này.");

        var cls = new Class
        {
            Name = dto.Name,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            SemesterId = dto.SemesterId,
            SlotId = dto.SlotId,
            Capacity = dto.Capacity,
            Enrolled = 0,
            Status = 1,
            RoomNumber = dto.RoomNumber,
            CreatedAt = DateTime.Now
        };
        await _classRepo.AddAsync(cls);
        var created = await _classRepo.GetWithDetailsAsync(cls.Id);
        return ServiceResult<ClassDto>.Success(MapToDto(created!), "Tạo lớp học thành công!");
    }

    public async Task<ServiceResult<ClassDto>> UpdateAsync(int id, UpdateClassDto dto)
    {
        var cls = await _classRepo.GetByIdAsync(id);
        if (cls == null) return ServiceResult<ClassDto>.Failure("Không tìm thấy lớp học.");
        if (cls.Status != 1) return ServiceResult<ClassDto>.Failure("Chỉ có thể chỉnh sửa lớp đang ở trạng thái 'Đang mở'.");

        cls.Name = dto.Name;
        cls.TeacherId = dto.TeacherId;
        cls.SlotId = dto.SlotId;
        cls.Capacity = dto.Capacity;
        cls.RoomNumber = dto.RoomNumber;
        await _classRepo.UpdateAsync(cls);
        var updated = await _classRepo.GetWithDetailsAsync(id);
        return ServiceResult<ClassDto>.Success(MapToDto(updated!), "Cập nhật lớp học thành công!");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var cls = await _classRepo.GetByIdAsync(id);
        if (cls == null) return ServiceResult.Failure("Không tìm thấy lớp học.");
        if (cls.Status != 1) return ServiceResult.Failure("Chỉ có thể xóa lớp đang ở trạng thái 'Đang mở'.");
        if (cls.Enrolled > 0) return ServiceResult.Failure("Không thể xóa lớp đã có sinh viên đăng ký.");

        await _classRepo.DeleteAsync(cls);
        return ServiceResult.Success("Đã xóa lớp học.");
    }

    public async Task<ServiceResult<ClassDto>> ApproveAsync(int id)
    {
        var cls = await _classRepo.GetWithDetailsAsync(id);
        if (cls == null) return ServiceResult<ClassDto>.Failure("Không tìm thấy lớp học.");
        if (cls.Status != 1) return ServiceResult<ClassDto>.Failure("Chỉ có thể phê duyệt lớp đang ở trạng thái 'Đang mở'.");

        cls.Status = 2;
        await _classRepo.UpdateAsync(cls);
        return ServiceResult<ClassDto>.Success(MapToDto(cls), "Đã phê duyệt lớp học. Lớp chuyển sang trạng thái 'Đang học'.");
    }

    public async Task<ServiceResult<ClassDto>> CancelAsync(int id, string? reason = null)
    {
        var cls = await _classRepo.GetWithDetailsAsync(id);
        if (cls == null) return ServiceResult<ClassDto>.Failure("Không tìm thấy lớp học.");
        if (cls.Status is not (1 or 2)) return ServiceResult<ClassDto>.Failure("Không thể hủy lớp học ở trạng thái này.");

        cls.Status = 0;
        await _classRepo.UpdateAsync(cls);
        return ServiceResult<ClassDto>.Success(MapToDto(cls), "Đã hủy lớp học.");
    }

    public async Task<List<SlotDto>> GetAllSlotsAsync()
    {
        var slots = await _slotRepo.GetAllOrderedAsync();
        return slots.Select(s => new SlotDto
        {
            Id = s.Id,
            Name = s.Name,
            DayOfWeek = s.DayOfWeek,
            StartTime = s.StartTime,
            EndTime = s.EndTime
        }).ToList();
    }

    public async Task<bool> HasScheduleConflictAsync(int semesterId, int slotId, int? excludeClassId = null)
    {
        return await _classRepo.AnyAsync(c =>
            c.SemesterId == semesterId &&
            c.SlotId == slotId &&
            c.Status != 0 &&
            (excludeClassId == null || c.Id != excludeClassId));
    }

    private static ClassDto MapToDto(Class c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        SubjectId = c.SubjectId,
        SubjectCode = c.Subject?.Code ?? "",
        SubjectName = c.Subject?.Name ?? "",
        TeacherId = c.TeacherId,
        TeacherName = c.Teacher?.FullName ?? "",
        SemesterId = c.SemesterId,
        SemesterName = c.Semester?.Name ?? "",
        SlotId = c.SlotId,
        SlotName = c.Slot?.Name ?? "",
        DayOfWeek = c.Slot?.DayOfWeek ?? 0,
        StartTime = c.Slot?.StartTime ?? TimeSpan.Zero,
        EndTime = c.Slot?.EndTime ?? TimeSpan.Zero,
        Capacity = c.Capacity,
        Enrolled = c.Enrolled,
        Status = c.Status,
        RoomNumber = c.RoomNumber,
        CreatedAt = c.CreatedAt
    };
}
