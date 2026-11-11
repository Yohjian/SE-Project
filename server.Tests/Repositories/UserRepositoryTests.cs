using Microsoft.AspNetCore.Identity;
using NSubstitute;
using QuattroLingo.Entities;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            _userManager = Substitute.For<UserManager<ApplicationUser>>(
                Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _repository = new UserRepository(_userManager);
        }



        [Fact]
        public async Task FindByEmailAsync_KnownEmail_ReturnsUserFromUserManager()
        {
            var user = new ApplicationUser { Id = "user-1", Email = "ona@example.com" };
            _userManager.FindByEmailAsync("ona@example.com").Returns(user);

            Assert.Same(user, await _repository.FindByEmailAsync("ona@example.com"));
        }

        [Fact]
        public async Task FindByEmailAsync_UnknownEmail_ReturnsNull()
        {
            _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

            Assert.Null(await _repository.FindByEmailAsync("nobody@example.com"));
        }



        [Fact]
        public async Task CreateAsync_NewUser_PassesUserAndPasswordAndReturnsIdentityResult()
        {
            var user = new ApplicationUser { Email = "ona@example.com" };
            _userManager.CreateAsync(user, "Password123").Returns(IdentityResult.Success);

            var result = await _repository.CreateAsync(user, "Password123");

            Assert.Same(IdentityResult.Success, result);
            await _userManager.Received(1).CreateAsync(user, "Password123");
        }

        [Fact]
        public async Task CreateAsync_UserManagerRejectsPassword_ReturnsFailedResult()
        {
            var failed = IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Too short." });
            _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "short").Returns(failed);

            var result = await _repository.CreateAsync(new ApplicationUser(), "short");

            Assert.False(result.Succeeded);
            Assert.Equal("PasswordTooShort", Assert.Single(result.Errors).Code);
        }



        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CheckPasswordAsync_AnyPassword_ReturnsUserManagerVerdict(bool valid)
        {
            var user = new ApplicationUser { Id = "user-1" };
            _userManager.CheckPasswordAsync(user, "Password123").Returns(valid);

            Assert.Equal(valid, await _repository.CheckPasswordAsync(user, "Password123"));
        }
    }
}
