namespace TeamA.ToDo.Core.Models.General;

public class AppConfig
{
    public string AppName { get; set; } = "Todo App";
    public string BaseUrl { get; set; } = "https://yourdomain.com";
    public string SupportEmail { get; set; } = "support@yourdomain.com";
    public string LogoUrl { get; set; } = "/images/logo.png";

    // Seeding configuration
    public bool SeedSampleData { get; set; } = true;
    public string DefaultAdminEmail { get; set; } = "admin@todo.com";
    public string DefaultAdminPassword { get; set; } = "@likh0rs4nD";
    public int TaskArchiveDays { get; set; } = 90;
    public bool EnableEmailNotifications { get; set; } = true;
}