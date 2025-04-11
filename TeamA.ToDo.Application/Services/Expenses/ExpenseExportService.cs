// Add new file: TeamA.ToDo.Application/Services/Expenses/ExpenseExportService.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.EntityFramework;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace TeamA.ToDo.Application.Services.Expenses
{
    public class ExpenseExportService : IExpenseExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExpenseExportService> _logger;

        public ExpenseExportService(
            ApplicationDbContext context,
            ILogger<ExpenseExportService> logger)
        {
            _context = context;
            _logger = logger;

            // Set the LicenseContext (for non-commercial use)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ServiceResponse<byte[]>> ExportExpensesToCsvAsync(string userId, DateTime? startDate, DateTime? endDate)
        {
            var response = new ServiceResponse<byte[]>();

            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                // Fetch expenses
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Where(e => e.UserId == userId && e.Date >= start && e.Date <= end)
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

                // Create CSV content
                var csvBuilder = new StringBuilder();

                // Add header
                csvBuilder.AppendLine("Id,Date,Description,Amount,Category,PaymentMethod,IsRecurring,Tags");

                // Add data rows
                foreach (var expense in expenses)
                {
                    var tags = string.Join(";", expense.Tags.Select(t => t.Name));

                    csvBuilder.AppendLine(string.Join(",",
                        expense.Id,
                        expense.Date.ToString("yyyy-MM-dd"),
                        $"\"{expense.Description?.Replace("\"", "\"\"")}\"", // Escape quotes in description
                        expense.Amount,
                        $"\"{expense.Category?.Name ?? "Uncategorized"}\"",
                        $"\"{expense.PaymentMethod?.Name ?? "Cash"}\"",
                        expense.IsRecurring ? "Yes" : "No",
                        $"\"{tags}\""
                    ));
                }

                response.Data = Encoding.UTF8.GetBytes(csvBuilder.ToString());
                response.Message = $"Successfully exported {expenses.Count} expenses to CSV";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting expenses to CSV");
                response.Success = false;
                response.Message = "Failed to export expenses to CSV";
                return response;
            }
        }

        public async Task<ServiceResponse<byte[]>> ExportExpensesToExcelAsync(string userId, DateTime? startDate, DateTime? endDate)
        {
            var response = new ServiceResponse<byte[]>();

            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                // Fetch expenses
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Include(e => e.Tags)
                    .Where(e => e.UserId == userId && e.Date >= start && e.Date <= end)
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

                // Create Excel package
                using (var package = new ExcelPackage())
                {
                    // Add a worksheet
                    var worksheet = package.Workbook.Worksheets.Add("Expenses");

                    // Style the header
                    worksheet.Cells["A1:H1"].Style.Font.Bold = true;
                    worksheet.Cells["A1:H1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells["A1:H1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Add header row
                    worksheet.Cells[1, 1].Value = "Date";
                    worksheet.Cells[1, 2].Value = "Description";
                    worksheet.Cells[1, 3].Value = "Amount";
                    worksheet.Cells[1, 4].Value = "Category";
                    worksheet.Cells[1, 5].Value = "Payment Method";
                    worksheet.Cells[1, 6].Value = "Recurring";
                    worksheet.Cells[1, 7].Value = "Tags";
                    worksheet.Cells[1, 8].Value = "ID";

                    // Add data rows
                    int row = 2;
                    foreach (var expense in expenses)
                    {
                        worksheet.Cells[row, 1].Value = expense.Date;
                        worksheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";

                        worksheet.Cells[row, 2].Value = expense.Description;

                        worksheet.Cells[row, 3].Value = expense.Amount;
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";

                        worksheet.Cells[row, 4].Value = expense.Category?.Name ?? "Uncategorized";
                        worksheet.Cells[row, 5].Value = expense.PaymentMethod?.Name ?? "Cash";
                        worksheet.Cells[row, 6].Value = expense.IsRecurring ? "Yes" : "No";

                        var tags = string.Join(", ", expense.Tags.Select(t => t.Name));
                        worksheet.Cells[row, 7].Value = tags;

                        worksheet.Cells[row, 8].Value = expense.Id.ToString();

                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Set column widths to reasonable values
                    worksheet.Column(2).Width = 40; // Description
                    worksheet.Column(7).Width = 30; // Tags
                    worksheet.Column(8).Width = 36; // ID

                    // Add totals at the bottom
                    worksheet.Cells[row, 2].Value = "Total:";
                    worksheet.Cells[row, 2].Style.Font.Bold = true;

                    worksheet.Cells[row, 3].Formula = $"SUM(C2:C{row - 1})";
                    worksheet.Cells[row, 3].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";

                    // Get the Excel binary data
                    response.Data = package.GetAsByteArray();
                }

                response.Message = $"Successfully exported {expenses.Count} expenses to Excel";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting expenses to Excel");
                response.Success = false;
                response.Message = "Failed to export expenses to Excel";
                return response;
            }
        }

        public async Task<ServiceResponse<byte[]>> ExportBudgetsToExcelAsync(string userId)
        {
            var response = new ServiceResponse<byte[]>();

            try
            {
                // Fetch budgets
                var budgets = await _context.Budgets
                    .Include(b => b.Category)
                    .Where(b => b.UserId == userId)
                    .OrderBy(b => b.StartDate)
                    .ToListAsync();

                // Fetch expenses for calculating spend amounts
                var earliestStartDate = budgets.Min(b => b.StartDate);
                var latestEndDate = budgets.Max(b => b.EndDate);

                var expenses = await _context.Expenses
                    .Where(e => e.UserId == userId &&
                               e.Date >= earliestStartDate &&
                               e.Date <= latestEndDate)
                    .ToListAsync();

                // Create Excel package
                using (var package = new ExcelPackage())
                {
                    // Add a worksheet
                    var worksheet = package.Workbook.Worksheets.Add("Budgets");

                    // Style the header
                    worksheet.Cells["A1:I1"].Style.Font.Bold = true;
                    worksheet.Cells["A1:I1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells["A1:I1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Add header row
                    worksheet.Cells[1, 1].Value = "Name";
                    worksheet.Cells[1, 2].Value = "Category";
                    worksheet.Cells[1, 3].Value = "Start Date";
                    worksheet.Cells[1, 4].Value = "End Date";
                    worksheet.Cells[1, 5].Value = "Period";
                    worksheet.Cells[1, 6].Value = "Budget Amount";
                    worksheet.Cells[1, 7].Value = "Spent Amount";
                    worksheet.Cells[1, 8].Value = "Remaining";
                    worksheet.Cells[1, 9].Value = "% Used";

                    // Add data rows
                    int row = 2;
                    foreach (var budget in budgets)
                    {
                        // Calculate spent amount for this budget
                        decimal spentAmount = expenses
                            .Where(e => e.CategoryId == budget.CategoryId &&
                                     e.Date >= budget.StartDate &&
                                     e.Date <= budget.EndDate)
                            .Sum(e => e.Amount);

                        decimal remainingAmount = budget.Amount - spentAmount;
                        double percentUsed = budget.Amount > 0 ?
                            (double)(spentAmount / budget.Amount) * 100 : 0;

                        worksheet.Cells[row, 1].Value = budget.Name;
                        worksheet.Cells[row, 2].Value = budget.Category?.Name ?? "Uncategorized";

                        worksheet.Cells[row, 3].Value = budget.StartDate;
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "yyyy-mm-dd";

                        worksheet.Cells[row, 4].Value = budget.EndDate;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "yyyy-mm-dd";

                        worksheet.Cells[row, 5].Value = budget.Period.ToString();

                        worksheet.Cells[row, 6].Value = budget.Amount;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "$#,##0.00";

                        worksheet.Cells[row, 7].Value = spentAmount;
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "$#,##0.00";

                        worksheet.Cells[row, 8].Value = remainingAmount;
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "$#,##0.00";

                        worksheet.Cells[row, 9].Value = percentUsed;
                        worksheet.Cells[row, 9].Style.Numberformat.Format = "0.00%";

                        // Conditional formatting for % used
                        if (percentUsed >= 100)
                        {
                            worksheet.Cells[row, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                        }
                        else if (percentUsed >= 90)
                        {
                            worksheet.Cells[row, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                        }

                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Get the Excel binary data
                    response.Data = package.GetAsByteArray();
                }

                response.Message = $"Successfully exported {budgets.Count} budgets to Excel";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting budgets to Excel");
                response.Success = false;
                response.Message = "Failed to export budgets to Excel";
                return response;
            }
        }

        public async Task<ServiceResponse<byte[]>> ExportMonthlyReportToExcelAsync(string userId, int year, int? month)
        {
            var response = new ServiceResponse<byte[]>();

            try
            {
                DateTime startDate;
                DateTime endDate;
                string reportTitle;

                if (month.HasValue)
                {
                    // Export for specific month
                    startDate = new DateTime(year, month.Value, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    reportTitle = $"Expenses for {startDate:MMMM yyyy}";
                }
                else
                {
                    // Export for entire year
                    startDate = new DateTime(year, 1, 1);
                    endDate = new DateTime(year, 12, 31);
                    reportTitle = $"Expenses for {year}";
                }

                // Fetch expenses
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Where(e => e.UserId == userId &&
                               e.Date >= startDate &&
                               e.Date <= endDate)
                    .OrderBy(e => e.Date)
                    .ToListAsync();

                // Calculate category totals
                var categoryTotals = expenses
                    .GroupBy(e => e.Category?.Name ?? "Uncategorized")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Total = g.Sum(e => e.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Total)
                    .ToList();

                // Calculate payment method totals
                var paymentMethodTotals = expenses
                    .GroupBy(e => e.PaymentMethod?.Name ?? "Cash")
                    .Select(g => new
                    {
                        PaymentMethod = g.Key,
                        Total = g.Sum(e => e.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(p => p.Total)
                    .ToList();

                // Calculate monthly totals if exporting yearly report
                List<(string Month, decimal Total, int Count)> monthlyTotals = null;
                if (!month.HasValue)
                {
                    monthlyTotals = expenses
                        .GroupBy(e => e.Date.Month)
                        .Select(g => (
                            Month: new DateTime(year, g.Key, 1).ToString("MMMM"),
                            Total: g.Sum(e => e.Amount),
                            Count: g.Count()
                        ))
                        .OrderBy(m => DateTime.ParseExact(m.Month, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month)
                        .ToList();
                }

                // Create Excel package
                using (var package = new ExcelPackage())
                {
                    // Create summary worksheet
                    var summarySheet = package.Workbook.Worksheets.Add("Summary");

                    // Add title
                    summarySheet.Cells["A1:C1"].Merge = true;
                    summarySheet.Cells["A1"].Value = reportTitle;
                    summarySheet.Cells["A1"].Style.Font.Bold = true;
                    summarySheet.Cells["A1"].Style.Font.Size = 14;

                    // Add report overview
                    summarySheet.Cells["A3"].Value = "Total Expenses:";
                    summarySheet.Cells["B3"].Value = expenses.Sum(e => e.Amount);
                    summarySheet.Cells["B3"].Style.Numberformat.Format = "$#,##0.00";

                    summarySheet.Cells["A4"].Value = "Number of Expenses:";
                    summarySheet.Cells["B4"].Value = expenses.Count;

                    summarySheet.Cells["A5"].Value = "Average Expense:";
                    summarySheet.Cells["B5"].Value = expenses.Count > 0 ?
                        expenses.Sum(e => e.Amount) / expenses.Count : 0;
                    summarySheet.Cells["B5"].Style.Numberformat.Format = "$#,##0.00";

                    // Add category breakdown
                    summarySheet.Cells["A7"].Value = "Expense by Category";
                    summarySheet.Cells["A7"].Style.Font.Bold = true;

                    summarySheet.Cells["A8"].Value = "Category";
                    summarySheet.Cells["B8"].Value = "Amount";
                    summarySheet.Cells["C8"].Value = "Count";
                    summarySheet.Cells["D8"].Value = "% of Total";

                    summarySheet.Cells["A8:D8"].Style.Font.Bold = true;
                    summarySheet.Cells["A8:D8"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    summarySheet.Cells["A8:D8"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    int row = 9;
                    decimal totalAmount = expenses.Sum(e => e.Amount);

                    foreach (var category in categoryTotals)
                    {
                        summarySheet.Cells[row, 1].Value = category.Category;

                        summarySheet.Cells[row, 2].Value = category.Total;
                        summarySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";

                        summarySheet.Cells[row, 3].Value = category.Count;

                        summarySheet.Cells[row, 4].Value = totalAmount > 0 ?
                            (double)(category.Total / totalAmount) : 0;
                        summarySheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";

                        row++;
                    }

                    // Add payment method breakdown
                    row += 2;
                    summarySheet.Cells[row, 1].Value = "Expense by Payment Method";
                    summarySheet.Cells[row, 1].Style.Font.Bold = true;

                    row++;
                    summarySheet.Cells[row, 1].Value = "Payment Method";
                    summarySheet.Cells[row, 2].Value = "Amount";
                    summarySheet.Cells[row, 3].Value = "Count";
                    summarySheet.Cells[row, 4].Value = "% of Total";

                    summarySheet.Cells[row, 1, row, 4].Style.Font.Bold = true;
                    summarySheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    summarySheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    row++;
                    foreach (var paymentMethod in paymentMethodTotals)
                    {
                        summarySheet.Cells[row, 1].Value = paymentMethod.PaymentMethod;

                        summarySheet.Cells[row, 2].Value = paymentMethod.Total;
                        summarySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";

                        summarySheet.Cells[row, 3].Value = paymentMethod.Count;

                        summarySheet.Cells[row, 4].Value = totalAmount > 0 ?
                            (double)(paymentMethod.Total / totalAmount) : 0;
                        summarySheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";

                        row++;
                    }

                    // Add monthly breakdown for yearly reports
                    if (monthlyTotals != null)
                    {
                        row += 2;
                        summarySheet.Cells[row, 1].Value = "Monthly Breakdown";
                        summarySheet.Cells[row, 1].Style.Font.Bold = true;

                        row++;
                        summarySheet.Cells[row, 1].Value = "Month";
                        summarySheet.Cells[row, 2].Value = "Amount";
                        summarySheet.Cells[row, 3].Value = "Count";
                        summarySheet.Cells[row, 4].Value = "% of Total";

                        summarySheet.Cells[row, 1, row, 4].Style.Font.Bold = true;
                        summarySheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        summarySheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                        row++;
                        foreach (var data in monthlyTotals)
                        {
                            summarySheet.Cells[row, 1].Value = data.Month;

                            summarySheet.Cells[row, 2].Value = data.Total;
                            summarySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";

                            summarySheet.Cells[row, 3].Value = data.Count;

                            summarySheet.Cells[row, 4].Value = totalAmount > 0 ?
                                (double)(data.Total / totalAmount) : 0;
                            summarySheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";

                            row++;
                        }
                    }

                    // Auto-fit columns
                    summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

                    // Create expenses worksheet
                    var expensesSheet = package.Workbook.Worksheets.Add("Expenses");

                    // Add header row
                    expensesSheet.Cells[1, 1].Value = "Date";
                    expensesSheet.Cells[1, 2].Value = "Description";
                    expensesSheet.Cells[1, 3].Value = "Amount";
                    expensesSheet.Cells[1, 4].Value = "Category";
                    expensesSheet.Cells[1, 5].Value = "Payment Method";
                    expensesSheet.Cells[1, 6].Value = "Recurring";

                    expensesSheet.Cells[1, 1, 1, 6].Style.Font.Bold = true;
                    expensesSheet.Cells[1, 1, 1, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    expensesSheet.Cells[1, 1, 1, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Add data rows
                    row = 2;
                    foreach (var expense in expenses)
                    {
                        expensesSheet.Cells[row, 1].Value = expense.Date;
                        expensesSheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";

                        expensesSheet.Cells[row, 2].Value = expense.Description;

                        expensesSheet.Cells[row, 3].Value = expense.Amount;
                        expensesSheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";

                        expensesSheet.Cells[row, 4].Value = expense.Category?.Name ?? "Uncategorized";
                        expensesSheet.Cells[row, 5].Value = expense.PaymentMethod?.Name ?? "Cash";
                        expensesSheet.Cells[row, 6].Value = expense.IsRecurring ? "Yes" : "No";

                        row++;
                    }

                    // Add totals
                    expensesSheet.Cells[row, 2].Value = "Total:";
                    expensesSheet.Cells[row, 2].Style.Font.Bold = true;

                    expensesSheet.Cells[row, 3].Formula = $"SUM(C2:C{row - 1})";
                    expensesSheet.Cells[row, 3].Style.Font.Bold = true;
                    expensesSheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";

                    // Auto-fit columns
                    expensesSheet.Cells[expensesSheet.Dimension.Address].AutoFitColumns();

                    // Get the Excel binary data
                    response.Data = package.GetAsByteArray();
                }

                response.Message = $"Successfully exported expense report to Excel";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly report to Excel");
                response.Success = false;
                response.Message = "Failed to export monthly report to Excel";
                return response;
            }
        }

        public async Task<ServiceResponse<byte[]>> ExportCategoryReportToExcelAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var response = new ServiceResponse<byte[]>();

            try
            {
                // Fetch expenses
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId &&
                               e.Date >= startDate &&
                               e.Date <= endDate)
                    .ToListAsync();

                // Get all categories
                var categories = await _context.ExpenseCategories
                    .Where(c => c.UserId == userId || c.IsSystem)
                    .ToListAsync();

                // Add "Uncategorized" for expenses without a category
                var categoryIds = categories.Select(c => c.Id).ToList();
                if (expenses.Any(e => e.CategoryId == null || !categoryIds.Contains(e.CategoryId)))
                {
                    // Ensure we have an "Uncategorized" entry in our report
                }

                // Calculate expenses by category
                var categoryExpenses = expenses
                    .GroupBy(e => e.CategoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

                using (var package = new ExcelPackage())
                {
                    // Create summary worksheet
                    var summarySheet = package.Workbook.Worksheets.Add("Category Summary");

                    // Add title
                    summarySheet.Cells["A1:E1"].Merge = true;
                    summarySheet.Cells["A1"].Value = $"Category Report ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})";
                    summarySheet.Cells["A1"].Style.Font.Bold = true;
                    summarySheet.Cells["A1"].Style.Font.Size = 14;

                    // Add header row
                    summarySheet.Cells["A3"].Value = "Category";
                    summarySheet.Cells["B3"].Value = "Total Amount";
                    summarySheet.Cells["C3"].Value = "% of Total";
                    summarySheet.Cells["D3"].Value = "Number of Expenses";
                    summarySheet.Cells["E3"].Value = "Average Amount";

                    summarySheet.Cells["A3:E3"].Style.Font.Bold = true;
                    summarySheet.Cells["A3:E3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    summarySheet.Cells["A3:E3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Calculate total amount
                    decimal totalAmount = expenses.Sum(e => e.Amount);

                    // Add data rows
                    int row = 4;
                    foreach (var category in categories)
                    {
                        var categoryId = category.Id;
                        var catExpenses = categoryExpenses.ContainsKey(categoryId)
                            ? categoryExpenses[categoryId]
                            : new List<Core.Models.Expenses.Expense>();

                        var categoryTotal = catExpenses.Sum(e => e.Amount);
                        var categoryCount = catExpenses.Count;
                        var categoryAverage = categoryCount > 0 ? categoryTotal / categoryCount : 0;
                        var categoryPercentage = totalAmount > 0 ? (categoryTotal / totalAmount) : 0;

                        summarySheet.Cells[row, 1].Value = category.Name;

                        summarySheet.Cells[row, 2].Value = categoryTotal;
                        summarySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";

                        summarySheet.Cells[row, 3].Value = (double)categoryPercentage;
                        summarySheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";

                        summarySheet.Cells[row, 4].Value = categoryCount;

                        summarySheet.Cells[row, 5].Value = categoryAverage;
                        summarySheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";

                        row++;
                    }

                    // Add "Uncategorized" row if needed
                    var uncategorizedExpenses = expenses.Where(e => e.CategoryId == null).ToList();
                    if (uncategorizedExpenses.Any())
                    {
                        var uncatTotal = uncategorizedExpenses.Sum(e => e.Amount);
                        var uncatCount = uncategorizedExpenses.Count;
                        var uncatAverage = uncatCount > 0 ? uncatTotal / uncatCount : 0;
                        var uncatPercentage = totalAmount > 0 ? (uncatTotal / totalAmount) : 0;

                        summarySheet.Cells[row, 1].Value = "Uncategorized";

                        summarySheet.Cells[row, 2].Value = uncatTotal;
                        summarySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";

                        summarySheet.Cells[row, 3].Value = (double)uncatPercentage;
                        summarySheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";

                        summarySheet.Cells[row, 4].Value = uncatCount;

                        summarySheet.Cells[row, 5].Value = uncatAverage;
                        summarySheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";

                        row++;
                    }

                    // Add totals row
                    summarySheet.Cells[row, 1].Value = "Total";
                    summarySheet.Cells[row, 1].Style.Font.Bold = true;

                    summarySheet.Cells[row, 2].Value = totalAmount;
                    summarySheet.Cells[row, 2].Style.Font.Bold = true;
                    summarySheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";

                    summarySheet.Cells[row, 3].Value = 1.0; // 100%
                    summarySheet.Cells[row, 3].Style.Font.Bold = true;
                    summarySheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";

                    summarySheet.Cells[row, 4].Value = expenses.Count;
                    summarySheet.Cells[row, 4].Style.Font.Bold = true;

                    summarySheet.Cells[row, 5].Value = expenses.Count > 0 ? totalAmount / expenses.Count : 0;
                    summarySheet.Cells[row, 5].Style.Font.Bold = true;
                    summarySheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";

                    // Auto-fit columns
                    summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

                    // Create individual worksheets for each category
                    foreach (var category in categories)
                    {
                        // Limit worksheet name to 31 characters (Excel limit)
                        var worksheetName = category.Name;
                        if (worksheetName.Length > 31)
                        {
                            worksheetName = worksheetName.Substring(0, 28) + "...";
                        }

                        // Replace invalid worksheet name characters
                        worksheetName = worksheetName
                            .Replace("[", "(")
                            .Replace("]", ")")
                            .Replace("*", "")
                            .Replace("?", "")
                            .Replace(":", "-")
                            .Replace("/", "-")
                            .Replace("\\", "-");

                        var categorySheet = package.Workbook.Worksheets.Add(worksheetName);

                        // Add title
                        categorySheet.Cells["A1:D1"].Merge = true;
                        categorySheet.Cells["A1"].Value = $"Expenses for {category.Name}";
                        categorySheet.Cells["A1"].Style.Font.Bold = true;
                        categorySheet.Cells["A1"].Style.Font.Size = 14;

                        // Add header row
                        categorySheet.Cells["A3"].Value = "Date";
                        categorySheet.Cells["B3"].Value = "Description";
                        categorySheet.Cells["C3"].Value = "Amount";
                        categorySheet.Cells["D3"].Value = "Payment Method";

                        categorySheet.Cells["A3:D3"].Style.Font.Bold = true;
                        categorySheet.Cells["A3:D3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        categorySheet.Cells["A3:D3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                        // Get expenses for this category
                        var categoryId = category.Id;
                        var catExpenses = categoryExpenses.ContainsKey(categoryId)
                            ? categoryExpenses[categoryId]
                            : new List<Core.Models.Expenses.Expense>();

                        catExpenses = catExpenses
                            .OrderByDescending(e => e.Date)
                            .ToList();

                        // Add data rows
                        row = 4;
                        foreach (var expense in catExpenses)
                        {
                            categorySheet.Cells[row, 1].Value = expense.Date;
                            categorySheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";

                            categorySheet.Cells[row, 2].Value = expense.Description;

                            categorySheet.Cells[row, 3].Value = expense.Amount;
                            categorySheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";

                            categorySheet.Cells[row, 4].Value = expense.PaymentMethod?.Name ?? "Cash";

                            row++;
                        }

                        // Add totals row if there are any expenses
                        if (catExpenses.Any())
                        {
                            categorySheet.Cells[row, 2].Value = "Total:";
                            categorySheet.Cells[row, 2].Style.Font.Bold = true;

                            categorySheet.Cells[row, 3].Value = catExpenses.Sum(e => e.Amount);
                            categorySheet.Cells[row, 3].Style.Font.Bold = true;
                            categorySheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                        }

                        // Auto-fit columns
                        categorySheet.Cells[categorySheet.Dimension.Address].AutoFitColumns();
                    }

                    // Get the Excel binary data
                    response.Data = package.GetAsByteArray();
                }

                response.Message = $"Successfully exported category report to Excel";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting category report to Excel");
                response.Success = false;
                response.Message = "Failed to export category report to Excel";
                return response;
            }
        }
    }
}