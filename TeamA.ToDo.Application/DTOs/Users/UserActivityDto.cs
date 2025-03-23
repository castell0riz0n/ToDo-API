namespace TeamA.ToDo.Application.DTOs.Users;

public class UserActivityDto
{
    public string Action { get; set; }
    public string Description { get; set; }
    public string IpAddress { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime Timestamp { get; set; }
}