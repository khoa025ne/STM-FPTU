namespace FPT_SM.BLL.DTOs;

public class SubjectDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<PrerequisiteDto> Prerequisites { get; set; } = new();
    public List<SubjectClassDto> Classes { get; set; } = new(); // NEW: Classes using this subject
}

public class SubjectClassDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public string SemesterCode { get; set; } = null!;
    public int Enrolled { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = null!; // 0=Cancelled, 1=Open, 2=Ongoing, 3=Ended
}

public class PrerequisiteDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int PrerequisiteSubjectId { get; set; }
    public string PrerequisiteSubjectCode { get; set; } = null!;
    public string PrerequisiteSubjectName { get; set; } = null!;
}

public class CreateSubjectDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Credits { get; set; } = 3;
    public decimal Price { get; set; } = 6000000;
    public string? Description { get; set; }
    public List<int> PrerequisiteSubjectIds { get; set; } = new();
}

public class UpdateSubjectDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<int> PrerequisiteSubjectIds { get; set; } = new();
}
