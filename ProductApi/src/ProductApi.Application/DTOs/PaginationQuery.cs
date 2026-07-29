namespace ProductApi.Application.DTOs;

public class PaginationQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    /// <summary>Optional case-insensitive search term applied to ProductName.</summary>
    public string? Search { get; set; }
}
