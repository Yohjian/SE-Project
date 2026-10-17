using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuattroLingo.Exceptions;
using QuattroLingo.Middleware;

namespace QuattroLingo.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(DefaultHttpContext Context, string Body)> RunAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context, body);
    }

    [Fact]
    public async Task NoException_PassesThroughUntouched()
    {
        var (context, body) = await RunAsync(ctx =>
        {
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        Assert.Equal(204, context.Response.StatusCode);
        Assert.Equal("", body);
    }

    [Fact]
    public async Task NotFoundException_Returns404ProblemDetails()
    {
        var (context, body) = await RunAsync(_ => throw new NotFoundException("Set not found."));

        Assert.Equal(404, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Contains("Set not found.", body);
    }

    [Fact]
    public async Task ForbiddenException_Returns403()
    {
        var (context, _) = await RunAsync(_ => throw new ForbiddenException("Nope."));
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedException_Returns401()
    {
        var (context, _) = await RunAsync(_ => throw new UnauthorizedException("Bad login."));
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task ValidationException_Returns400WithFieldErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Email"] = ["Email is already taken."] };

        var (context, body) = await RunAsync(_ => throw new ValidationException("Failed.", errors));

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Contains("Email is already taken.", body);
    }

    [Fact]
    public async Task UnknownException_Returns500WithoutLeakingMessage()
    {
        var (context, body) = await RunAsync(_ => throw new InvalidOperationException("secret db password"));

        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain("secret db password", body);
    }
}