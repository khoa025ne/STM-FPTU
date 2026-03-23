namespace FPT_SM.BLL.Common;

public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Success(string? message = null) => new() { IsSuccess = true, Message = message };
    public static ServiceResult Failure(string message) => new() { IsSuccess = false, Message = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Success(T data, string? message = null) => new() { IsSuccess = true, Data = data, Message = message };
    public new static ServiceResult<T> Failure(string message) => new() { IsSuccess = false, Message = message };
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
