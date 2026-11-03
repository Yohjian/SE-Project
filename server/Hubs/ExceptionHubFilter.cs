using Microsoft.AspNetCore.SignalR;
using QuattroLingo.Exceptions;

namespace QuattroLingo.Hubs
{
    public class ExceptionHubFilter(ILogger<ExceptionHubFilter> logger) : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            try
            {
                return await next(invocationContext);
            }
            catch (Exception ex) when (ex is ValidationException or UnauthorizedException
                or ForbiddenException or NotFoundException or ConflictException)
            {
                logger.LogWarning("Hub call {Method} rejected: {Type}: {Message}",
                    invocationContext.HubMethodName, ex.GetType().Name, ex.Message);

                throw new HubException(ex.Message);
            }
        }
    }
}