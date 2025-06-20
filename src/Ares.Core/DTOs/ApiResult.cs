namespace Ares.Core.DTOs;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public ApiResult() { }

    public ApiResult(bool success, string? message = null, T? data = default)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public static ApiResult<T> Ok(T? data, string? message = null) => new(true, message, data);
    public static ApiResult<T> Fail(string message) => new(false, message, default);
}