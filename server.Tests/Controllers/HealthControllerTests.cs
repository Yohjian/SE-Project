using Microsoft.AspNetCore.Mvc;
using QuattroLingo.Controllers;
using Xunit;

namespace QuattroLingo.Tests.Controllers;

public class HealthControllerTests
{
    private readonly HealthController _controller = new();

    [Fact]
    public void Get_ReturnsOkWithStatusOk()
    {
        var result = _controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsAssignableFrom<object>(okResult.Value);
        var statusProp = value.GetType().GetProperty("status");

        Assert.NotNull(statusProp);
        Assert.Equal("ok", statusProp!.GetValue(value));
    }

    [Fact]
    public void Hello_ReturnsOkWithMessage()
    {
        var result = _controller.Hello();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsAssignableFrom<object>(okResult.Value);
        var messageProp = value.GetType().GetProperty("message");

        Assert.NotNull(messageProp);
        Assert.Equal("Backend loaded successfully.", messageProp!.GetValue(value));
    }
}