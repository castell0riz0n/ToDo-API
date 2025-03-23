namespace TodoApp.API.DTOs;

public class TaskStatisticsDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int NotStartedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DueTodayTasks { get; set; }
    public int UpcomingTasks { get; set; }
    public double CompletionRate { get; set; }
    public Dictionary<string, int> TasksByCategory { get; set; }
    public Dictionary<string, int> TasksByPriority { get; set; }
    public Dictionary<string, int> CompletionByWeekday { get; set; }
}