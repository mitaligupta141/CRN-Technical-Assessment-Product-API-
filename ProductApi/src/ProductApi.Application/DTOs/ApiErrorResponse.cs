namespace ProductApi.Application.DTOs;

/// <summary>
/// Consistent error envelope returned by the global exception middleware for
/// every non-2xx response, per the "consistent error response format" requirement.
/// </summary>
public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
