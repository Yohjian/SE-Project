using Microsoft.AspNetCore.Identity;
using QuattroLingo.Entity;
using QuattroLingo.Exceptions;
using QuattroLingo.Repository;
using QuattroLingo.Service;

namespace QuattroLingo.Tests.Services;

public class AuthServiceTests
{
    private class FakeUserRepository : IUserRepository
    {
        public readonly List<ApplicationUser> Users = [];
        private readonly Dictionary<string, string> _passwords = [];

        public Task<ApplicationUser?> FindByEmailAsync(string email) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

        public Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            if (Users.Any(u => u.Email == user.Email))
                return Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Code = "DuplicateUserName", Description = "Email is already taken." }));

            Users.Add(user);
            _passwords[user.Email!] = password;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) =>
            Task.FromResult(_passwords.TryGetValue(user.Email!, out var p) && p == password);
    }

    private class FakeTokenService : ITokenService
    {
        public string GenerateAuthToken(ApplicationUser user) => $"token-for-{user.Email}";
    }

    private readonly FakeUserRepository _repo = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_repo, new FakeTokenService());
    }

    [Fact]
    public async Task Register_CreatesActiveUserWithUserRole()
    {
        await _service.RegisterAsync("a@test.com", "Password1");

        var user = Assert.Single(_repo.Users);
        Assert.Equal("a@test.com", user.Email);
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsValidationException()
    {
        await _service.RegisterAsync("a@test.com", "Password1");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.RegisterAsync("a@test.com", "Password1"));

        Assert.Contains("DuplicateUserName", ex.Errors.Keys);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenEmailAndRole()
    {
        await _service.RegisterAsync("a@test.com", "Password1");

        var response = await _service.LoginAsync("a@test.com", "Password1");

        Assert.Equal("token-for-a@test.com", response.Token);
        Assert.Equal("a@test.com", response.Email);
        Assert.Equal("User", response.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await _service.RegisterAsync("a@test.com", "Password1");

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync("a@test.com", "WrongPassword1"));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync("nobody@test.com", "Password1"));
    }

    [Fact]
    public async Task Login_DeactivatedUser_ThrowsForbidden()
    {
        await _service.RegisterAsync("a@test.com", "Password1");
        _repo.Users[0].IsActive = false;

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.LoginAsync("a@test.com", "Password1"));
    }
}