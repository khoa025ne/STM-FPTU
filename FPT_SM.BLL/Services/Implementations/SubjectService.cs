using FPT_SM.BLL.Common;
using FPT_SM.BLL.DTOs;
using FPT_SM.BLL.Services.Interfaces;
using FPT_SM.DAL.Context;
using FPT_SM.DAL.Entities;
using FPT_SM.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FPT_SM.BLL.Services.Implementations;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepo;
    private readonly AppDbContext _context;

    public SubjectService(ISubjectRepository subjectRepo, AppDbContext context)
    {
        _subjectRepo = subjectRepo;
        _context = context;
    }

    public async Task<List<SubjectDto>> GetAllAsync()
    {
        var subjects = await _subjectRepo.GetAllWithPrerequisitesAsync();
        return subjects.Select(MapToDto).ToList();
    }

    public async Task<SubjectDto?> GetByIdAsync(int id)
    {
        var s = await _subjectRepo.GetWithPrerequisitesAsync(id);
        return s == null ? null : MapToDto(s);
    }

    public async Task<ServiceResult<SubjectDto>> CreateAsync(CreateSubjectDto dto)
    {
        if (await _subjectRepo.AnyAsync(s => s.Code == dto.Code))
            return ServiceResult<SubjectDto>.Failure($"Mã môn học '{dto.Code}' đã tồn tại.");

        if (dto.ProgramSemester is < 1 or > 9)
            return ServiceResult<SubjectDto>.Failure("Kỳ chương trình của môn học phải nằm trong khoảng 1 đến 9.");

        foreach (var prereqId in dto.PrerequisiteSubjectIds)
        {
            if (!await _subjectRepo.AnyAsync(s => s.Id == prereqId))
                return ServiceResult<SubjectDto>.Failure($"Không tìm thấy môn tiên quyết ID={prereqId}.");
        }

        var subject = new Subject
        {
            Code = dto.Code,
            Name = dto.Name,
            Credits = dto.Credits,
            Price = dto.Price,
            Description = dto.Description,
            ProgramSemester = dto.ProgramSemester,
            IsActive = true
        };
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        foreach (var prereqId in dto.PrerequisiteSubjectIds)
            _context.Prerequisites.Add(new Prerequisite { SubjectId = subject.Id, PrerequisiteSubjectId = prereqId });
        await _context.SaveChangesAsync();

        var created = await _subjectRepo.GetWithPrerequisitesAsync(subject.Id);
        return ServiceResult<SubjectDto>.Success(MapToDto(created!), "Tạo môn học thành công!");
    }

    public async Task<ServiceResult<SubjectDto>> UpdateAsync(int id, UpdateSubjectDto dto)
    {
        var subject = await _subjectRepo.GetWithPrerequisitesAsync(id);
        if (subject == null) return ServiceResult<SubjectDto>.Failure("Không tìm thấy môn học.");

        if (await _subjectRepo.AnyAsync(s => s.Code == dto.Code && s.Id != id))
            return ServiceResult<SubjectDto>.Failure($"Mã môn học '{dto.Code}' đã được sử dụng.");

        if (dto.ProgramSemester is < 1 or > 9)
            return ServiceResult<SubjectDto>.Failure("Kỳ chương trình của môn học phải nằm trong khoảng 1 đến 9.");

        foreach (var prereqId in dto.PrerequisiteSubjectIds)
        {
            if (await HasCircularDependency(id, prereqId))
                return ServiceResult<SubjectDto>.Failure("Không thể tạo môn tiên quyết gây ra vòng lặp.");
        }

        subject.Code = dto.Code;
        subject.Name = dto.Name;
        subject.Credits = dto.Credits;
        subject.Price = dto.Price;
        subject.Description = dto.Description;
        subject.ProgramSemester = dto.ProgramSemester;
        subject.IsActive = dto.IsActive;

        var existingPrereqs = _context.Prerequisites.Where(p => p.SubjectId == id).ToList();
        _context.Prerequisites.RemoveRange(existingPrereqs);
        foreach (var prereqId in dto.PrerequisiteSubjectIds)
            _context.Prerequisites.Add(new Prerequisite { SubjectId = id, PrerequisiteSubjectId = prereqId });

        await _context.SaveChangesAsync();

        var updated = await _subjectRepo.GetWithPrerequisitesAsync(id);
        return ServiceResult<SubjectDto>.Success(MapToDto(updated!), "Cập nhật môn học thành công!");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var subject = await _subjectRepo.GetByIdAsync(id);
        if (subject == null) return ServiceResult.Failure("Không tìm thấy môn học.");

        subject.IsActive = false;
        await _subjectRepo.UpdateAsync(subject);
        return ServiceResult.Success("Đã vô hiệu hóa môn học.");
    }

    public async Task<bool> HasCircularDependency(int subjectId, int prerequisiteId)
    {
        if (subjectId == prerequisiteId) return true;
        var prereqsOfPrereq = await _subjectRepo.GetPrerequisitesAsync(prerequisiteId);
        foreach (var p in prereqsOfPrereq)
        {
            if (await HasCircularDependency(subjectId, p.PrerequisiteSubjectId))
                return true;
        }
        return false;
    }

    private static SubjectDto MapToDto(Subject s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        Credits = s.Credits,
        Price = s.Price,
        Description = s.Description,
        IsActive = s.IsActive,
        ProgramSemester = s.ProgramSemester,
        Prerequisites = s.Prerequisites?.Select(p => new PrerequisiteDto
        {
            Id = p.Id,
            SubjectId = p.SubjectId,
            PrerequisiteSubjectId = p.PrerequisiteSubjectId,
            PrerequisiteSubjectCode = p.PrerequisiteSubject?.Code ?? "",
            PrerequisiteSubjectName = p.PrerequisiteSubject?.Name ?? ""
        }).ToList() ?? new(),
        Classes = s.Classes?.Select(c => new SubjectClassDto
        {
            ClassId = c.Id,
            ClassName = c.Name,
            SemesterName = c.Semester?.Name ?? "N/A",
            SemesterCode = c.Semester?.Code ?? "N/A",
            Enrolled = c.Enrolled,
            Capacity = c.Capacity,
            Status = GetClassStatusName(c.Status)
        }).ToList() ?? new()
    };

    private static string GetClassStatusName(int status) => status switch
    {
        0 => "Hủy",
        1 => "Mở",
        2 => "Đang diễn ra",
        3 => "Kết thúc",
        _ => "Không xác định"
    };
}
