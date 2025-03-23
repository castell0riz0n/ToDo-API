namespace TeamA.ToDo.Core.Models;

public class PermissionRequirement
{
    public HttpMethod Method { get; }
    public List<string> Roles { get; }
    public List<string> Permissions { get; }

    public PermissionRequirement(HttpMethod method, List<string> roles, List<string> permissions)
    {
        Method = method;
        Roles = roles ?? new List<string>();
        Permissions = permissions ?? new List<string>();
    }
}