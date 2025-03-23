namespace TeamA.ToDo.Application.DTOs.Email;

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; }
    public byte[] Content { get; set; }
    public string ContentType { get; set; }
}