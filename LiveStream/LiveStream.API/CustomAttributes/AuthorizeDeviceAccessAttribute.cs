using LiveStream.APPLICATION.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LiveStream.API.CustomAttributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeDeviceAccessAttribute : Attribute, IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var hub = invocationContext.Hub as StreamHub;
        var deviceIdParam = invocationContext.HubMethodArguments
            .FirstOrDefault(arg => arg?.GetType() == typeof(int)) as int?;

        if (deviceIdParam.HasValue && hub != null)
        {
            var userId = invocationContext.Context.UserIdentifier;
            if (!int.TryParse(userId, out int userIdInt))
            {
                throw new HubException("Invalid user ID");
            }

            var authService = invocationContext.ServiceProvider.GetRequiredService<IAuthorizationService>();
            if (!await authService.CanViewDeviceAsync(userIdInt, deviceIdParam.Value))
            {
                throw new HubException($"Access denied to device {deviceIdParam.Value}");
            }
        }

        return await next(invocationContext);
    }
}
