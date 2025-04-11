using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces.Expenses;

public interface IExpenseExportService
{
    Task<ServiceResponse<byte[]>> ExportExpensesToCsvAsync(string userId, DateTime? startDate, DateTime? endDate);
    Task<ServiceResponse<byte[]>> ExportExpensesToExcelAsync(string userId, DateTime? startDate, DateTime? endDate);
    Task<ServiceResponse<byte[]>> ExportBudgetsToExcelAsync(string userId);
    Task<ServiceResponse<byte[]>> ExportMonthlyReportToExcelAsync(string userId, int year, int? month);
    Task<ServiceResponse<byte[]>> ExportCategoryReportToExcelAsync(string userId, DateTime startDate, DateTime endDate);
}