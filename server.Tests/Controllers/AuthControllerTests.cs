using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuattroLingo.Controllers;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly IAuthService _auth = Substitute.For<IAuthService>();
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _controller = new AuthController(_auth).Anonymous();
        }



        [Fact]
        public async Task Register_ValidRequest_PassesEmailAndPasswordToService()
        {
            await _controller.Register(new Register("ona@example.com", "Password123"));

            await _auth.Received(1).RegisterAsync("ona@example.com", "Password123");
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsOkWithSuccessMessage()
        {
            var result = await _controller.Register(new Register("ona@example.com", "Password123"));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User registered successfully.", ok.Value.Property("message"));
        }

        [Fact]
        public async Task Register_ServiceRejectsEmail_LetsValidationExceptionBubbleToMiddleware()
        {
            _auth.RegisterAsync(Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new ValidationException("Email is already taken."));

            await Assert.ThrowsAsync<ValidationException>(
                () => _controller.Register(new Register("ona@example.com", "Password123")));
        }



        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithServiceResponse()
        {
            var response = new AuthResponse("jwt-token", "ona@example.com", "User");
            _auth.LoginAsync("ona@example.com", "Password123").Returns(response);

            var result = await _controller.Login(new Login("ona@example.com", "Password123"));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(response, ok.Value);
        }

        [Fact]
        public async Task Login_WrongCredentials_LetsUnauthorizedExceptionBubbleToMiddleware()
        {
            _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new UnauthorizedException("Invalid email or password."));

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => _controller.Login(new Login("ona@example.com", "wrong")));
        }



        [Fact]
        public void Me_SignedInUser_ReturnsIdEmailAndRoleFromToken()
        {
            _controller.WithUser("user-1", email: "ona@example.com", role: "Admin");

            var ok = Assert.IsType<OkObjectResult>(_controller.Me());

            Assert.Equal("user-1", ok.Value.Property("id"));
            Assert.Equal("ona@example.com", ok.Value.Property("email"));
            Assert.Equal("Admin", ok.Value.Property("role"));
        }

        [Fact]
        public void Me_TokenWithoutEmailOrRole_ReturnsNullForMissingClaims()
        {
            _controller.WithUser("user-1");

            var ok = Assert.IsType<OkObjectResult>(_controller.Me());

            Assert.Equal("user-1", ok.Value.Property("id"));
            Assert.Null(ok.Value.Property("email"));
            Assert.Null(ok.Value.Property("role"));
        }

        [Fact]
        public async Task Login_DeactivatedUser_LetsForbiddenExceptionBubbleToMiddleware()
        {
            _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new ForbiddenException("Account is deactivated."));

            await Assert.ThrowsAsync<ForbiddenException>(
                () => _controller.Login(new Login("ona@example.com", "Password123")));
        }



        [Theory]
        [InlineData(nameof(AuthController.Register), "POST", "api/auth/register")]
        [InlineData(nameof(AuthController.Login), "POST", "api/auth/login")]
        [InlineData(nameof(AuthController.Me), "GET", "api/auth/me")]
        public void RouteAttributes_EachAction_MapToExpectedVerbAndRoute(string action, string verb, string route)
        {
            Assert.Equal((verb, route), ControllerTestHelpers.RouteOf<AuthController>(action));
        }

        [Theory]
        [InlineData(nameof(AuthController.Register), false)]
        [InlineData(nameof(AuthController.Login), false)]
        [InlineData(nameof(AuthController.Me), true)]
        public void AuthorizeAttributes_EachAction_RequireLoginOnlyWhereExpected(string action, bool requiresLogin)
        {
            Assert.Equal(requiresLogin, ControllerTestHelpers.RequiresAuthorization<AuthController>(action));
        }
    }
}
