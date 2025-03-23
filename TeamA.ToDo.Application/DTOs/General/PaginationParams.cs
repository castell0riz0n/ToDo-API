namespace TeamA.ToDo.Application.DTOs.General;

public class PaginationParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = (value < 1) ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }

    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public string SearchTerm { get; set; }
}