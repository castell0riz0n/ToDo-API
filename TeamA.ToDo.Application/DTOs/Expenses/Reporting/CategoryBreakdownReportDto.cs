namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class CategoryBreakdownReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<CategoryBreakdownItemDto> Categories { get; set; }
}