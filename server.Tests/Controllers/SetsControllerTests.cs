using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Controllers;
using QuattroLingo.Data;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests
{
    public class SetsControllerTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static SetsController CreateController(AppDbContext db, string userId)
        {
            var controller = new SetsController(new SetService(new SetRepository(db)));

            var claims = new List<Claim>
            {
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            return controller;
        }

        [Fact]
        public async Task GetAllSets_ReturnsOnlyCurrentUsersSets()
        {
            await using var db = CreateDb();
            db.Sets.AddRange(
                new VocabularySet { UserId = "user-a", Name = "A's Set", CreatedAt = DateTime.UtcNow, Cards = [] },
                new VocabularySet { UserId = "user-b", Name = "B's Set", CreatedAt = DateTime.UtcNow, Cards = [] }
            );
            await db.SaveChangesAsync();

            var controller = CreateController(db, "user-a");
            var result = await controller.GetAllSets();

            var ok = Assert.IsType<OkObjectResult>(result);
            var sets = Assert.IsAssignableFrom<IEnumerable<SetSummary>>(ok.Value);
            Assert.Single(sets);
            Assert.Equal("A's Set", sets.First().Name);
        }

        [Fact]
        public async Task GetSet_OtherUsersSet_ThrowsNotFound()
        {
            await using var db = CreateDb();
            var otherUsersSet = new VocabularySet
            {
                UserId = "user-b",
                Name = "B's Set",
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };
            db.Sets.Add(otherUsersSet);
            await db.SaveChangesAsync();

            var controller = CreateController(db, "user-a");

            await Assert.ThrowsAsync<NotFoundException>(() => controller.GetSet(otherUsersSet.Id));
        }

        [Fact]
        public async Task GetSet_OwnSet_ReturnsOk()
        {
            await using var db = CreateDb();
            var set = new VocabularySet
            {
                UserId = "user-a",
                Name = "My Set",
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };
            db.Sets.Add(set);
            await db.SaveChangesAsync();

            var controller = CreateController(db, "user-a");
            var result = await controller.GetSet(set.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var contents = Assert.IsType<SetContents>(ok.Value);
            Assert.Equal("My Set", contents.Name);
        }

        [Fact]
        public async Task AddSet_AssignsCurrentUserAsOwner()
        {
            await using var db = CreateDb();
            var controller = CreateController(db, "user-a");

            var result = await controller.AddSet(new CreateSet("New Set"));

            Assert.IsType<OkObjectResult>(result);
            var stored = await db.Sets.FirstAsync();
            Assert.Equal("user-a", stored.UserId);
        }

        [Fact]
        public async Task AddCard_ToOtherUsersSet_ThrowsNotFound_AndSavesNothing()
        {
            await using var db = CreateDb();
            var otherUsersSet = new VocabularySet
            {
                UserId = "user-b",
                Name = "B's Set",
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };
            db.Sets.Add(otherUsersSet);
            await db.SaveChangesAsync();

            var controller = CreateController(db, "user-a");

            await Assert.ThrowsAsync<NotFoundException>(
                () => controller.AddCard(otherUsersSet.Id, new CreateCard("term", "definition")));

            Assert.Empty(db.Cards);
        }

        [Fact]
        public async Task AddCard_ToOwnSet_PersistsCard()
        {
            await using var db = CreateDb();
            var set = new VocabularySet
            {
                UserId = "user-a",
                Name = "My Set",
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };
            db.Sets.Add(set);
            await db.SaveChangesAsync();

            var controller = CreateController(db, "user-a");
            var result = await controller.AddCard(set.Id, new CreateCard("hello", "a greeting"));

            Assert.IsType<OkObjectResult>(result);
            var card = await db.Cards.FirstAsync();
            Assert.Equal("hello", card.Term);
            Assert.Equal(set.Id, card.SetId);
        }

        [Fact]
        public async Task GetCards_ForOtherUsersSet_ThrowsNotFound()
        {
            await using var db = CreateDb();
            var otherUsersSet = new VocabularySet
            {
                UserId = "user-b",
                Name = "B's Set",
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };
            db.Sets.Add(otherUsersSet);
            await db.SaveChangesAsync();

            db.Cards.Add(new VocabularyCard { Term = "secret", Definition = "shh", SetId = otherUsersSet.Id });
            await db.SaveChangesAsync();

            var controller = CreateController(db, "user-a");

            await Assert.ThrowsAsync<NotFoundException>(() => controller.GetCards(otherUsersSet.Id));
        }
    }
}