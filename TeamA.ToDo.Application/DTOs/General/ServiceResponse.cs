namespace TeamA.ToDo.Application.DTOs.General;

public class ServiceResponse<T>
{
    public T Data { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; }
    public List<string> Errors { get; set; }

    // Only for development environment
    public object DevNotes { get; set; }
}
