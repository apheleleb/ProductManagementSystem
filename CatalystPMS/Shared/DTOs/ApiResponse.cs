namespace CatalystPMS.Shared.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string error) => new()
    {
        Success = false,
        Errors = new List<string> { error }
    };

    public static ApiResponse<T> Fail(List<string> errors) => new()
    {
        Success = false,
        Errors = errors
    };
}

// Non-generic version for responses with no data payload
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse OkNoData(string message) => new()
    {
        Success = true,
        Message = message
    };

    public static new ApiResponse Fail(string error) => new()
    {
        Success = false,
        Errors = new List<string> { error }
    };
}