using LiveStream.DOMAIN.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LiveStream.API.CustomAttributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public Permission Permission { get; }

    public AuthorizePermissionAttribute(Permission permission)
    {
        Permission = permission;
        Policy = $"Permission:{permission}";
    }
}
