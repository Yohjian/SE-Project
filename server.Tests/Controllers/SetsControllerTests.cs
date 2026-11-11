using System.Text;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuattroLingo.Controllers;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Tests.Controllers
{
    public class SetsControllerTests
    {
        private const string UserId = "user-1";

        private readonly ISetService _sets = Substitute.For<ISetService>();
        private readonly SetsController _controller;

        public SetsControllerTests()
        {
            _controller = new SetsController(_sets).WithUser(UserId);
        }

        private static SetSummary MakeSummary(int id = 1, string name = "My Set") =>
            new(id, name, 0, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));



        [Fact]
        public async Task GetAllSets_SignedInUser_ReturnsOkWithThatUsersSets()
        {
            var sets = new List<SetSummary> { MakeSummary(1, "A"), MakeSummary(2, "B") };
            _sets.GetAllAsync(UserId).Returns(sets);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetAllSets());

            Assert.Same(sets, ok.Value);
        }



        [Fact]
        public async Task GetSet_OwnSet_ReturnsOkWithSetContents()
        {
            var contents = new SetContents(4, "My Set", [new CardResponse(1, "labas", "hello")]);
            _sets.GetAsync(UserId, 4).Returns(contents);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetSet(4));

            Assert.Same(contents, ok.Value);
        }

        [Fact]
        public async Task GetSet_ServiceThrowsNotFound_LetsExceptionBubbleToMiddleware()
        {
            _sets.GetAsync(UserId, 4).ThrowsAsync(new NotFoundException("Set not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetSet(4));
        }



        [Fact]
        public async Task AddSet_SignedInUser_CreatesSetOwnedByThatUser()
        {
            _sets.CreateAsync(UserId, "New Set").Returns(MakeSummary(name: "New Set"));

            await _controller.AddSet(new CreateSet("New Set"));

            await _sets.Received(1).CreateAsync(UserId, "New Set");
        }

        [Fact]
        public async Task AddSet_SignedInUser_ReturnsOkWithCreatedSummary()
        {
            var summary = MakeSummary(name: "New Set");
            _sets.CreateAsync(UserId, "New Set").Returns(summary);

            var ok = Assert.IsType<OkObjectResult>(await _controller.AddSet(new CreateSet("New Set")));

            Assert.Same(summary, ok.Value);
        }



        [Fact]
        public async Task DeleteSet_OwnSet_PassesUserIdAndSetIdToService()
        {
            await _controller.DeleteSet(4);

            await _sets.Received(1).DeleteSetAsync(UserId, 4);
        }

        [Fact]
        public async Task DeleteSet_OwnSet_ReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.DeleteSet(4));
        }



        [Fact]
        public async Task DeleteCard_OwnCard_PassesUserIdSetIdAndCardIdToService()
        {
            await _controller.DeleteCard(4, 9);

            await _sets.Received(1).DeleteCardAsync(UserId, 4, 9);
        }

        [Fact]
        public async Task DeleteCard_OwnCard_ReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.DeleteCard(4, 9));
        }



        [Fact]
        public async Task AddCard_OwnSet_PassesTermAndDefinitionToService()
        {
            _sets.AddCardAsync(UserId, 4, "labas", "hello").Returns(new CardResponse(9, "labas", "hello"));

            await _controller.AddCard(4, new CreateCard("labas", "hello"));

            await _sets.Received(1).AddCardAsync(UserId, 4, "labas", "hello");
        }

        [Fact]
        public async Task AddCard_OwnSet_ReturnsOkWithCreatedCard()
        {
            var card = new CardResponse(9, "labas", "hello");
            _sets.AddCardAsync(UserId, 4, "labas", "hello").Returns(card);

            var ok = Assert.IsType<OkObjectResult>(await _controller.AddCard(4, new CreateCard("labas", "hello")));

            Assert.Same(card, ok.Value);
        }

        [Fact]
        public async Task AddCard_ServiceThrowsNotFound_LetsExceptionBubbleToMiddleware()
        {
            _sets.AddCardAsync(UserId, 4, Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new NotFoundException("Set not found."));

            await Assert.ThrowsAsync<NotFoundException>(
                () => _controller.AddCard(4, new CreateCard("labas", "hello")));
        }



        [Fact]
        public async Task GetCards_OwnSet_ReturnsOkWithCards()
        {
            var cards = new List<CardResponse> { new(1, "labas", "hello"), new(2, "ačiū", "thanks") };
            _sets.GetCardsAsync(UserId, 4).Returns(cards);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetCards(4));

            Assert.Same(cards, ok.Value);
        }



        [Theory]
        [InlineData(FileType.Json, "application/json", "My Set.json")]
        [InlineData(FileType.Csv, "text/csv", "My Set.csv")]
        public async Task DownloadSet_OwnSet_ReturnsFileWithServiceContentTypeAndName(
            FileType fileType, string contentType, string fileName)
        {
            var content = new MemoryStream(Encoding.UTF8.GetBytes("data"));
            _sets.DownloadSetAsync(UserId, 4, fileType).Returns(new DownloadFile(content, contentType, fileName));

            var file = Assert.IsType<FileStreamResult>(await _controller.DownloadSet(4, fileType));

            Assert.Same(content, file.FileStream);
            Assert.Equal(contentType, file.ContentType);
            Assert.Equal(fileName, file.FileDownloadName);
        }

        [Fact]
        public async Task DownloadSet_UnsupportedFileType_LetsExceptionBubbleToMiddleware()
        {
            _sets.DownloadSetAsync(UserId, 4, (FileType)99)
                .ThrowsAsync(new UnsupportedFileTypeException("Unsupported file type."));

            await Assert.ThrowsAsync<UnsupportedFileTypeException>(() => _controller.DownloadSet(4, (FileType)99));
        }



        public static TheoryData<string, Func<SetsController, Task<IActionResult>>> EveryAction => new()
        {
            { nameof(SetsController.GetAllSets), c => c.GetAllSets() },
            { nameof(SetsController.GetSet), c => c.GetSet(4) },
            { nameof(SetsController.AddSet), c => c.AddSet(new CreateSet("New Set")) },
            { nameof(SetsController.DeleteSet), c => c.DeleteSet(4) },
            { nameof(SetsController.DeleteCard), c => c.DeleteCard(4, 9) },
            { nameof(SetsController.AddCard), c => c.AddCard(4, new CreateCard("labas", "hello")) },
            { nameof(SetsController.GetCards), c => c.GetCards(4) },
            { nameof(SetsController.DownloadSet), c => c.DownloadSet(4, FileType.Csv) }
        };

        [Theory]
        [MemberData(nameof(EveryAction))]
        public async Task AnyAction_TokenWithoutUserId_ThrowsUnauthorizedAndCallsNoService(
            string action, Func<SetsController, Task<IActionResult>> call)
        {
            _controller.WithUser(userId: null);

            await Assert.ThrowsAsync<UnauthorizedException>(() => call(_controller));
            Assert.False(_sets.ReceivedCalls().Any(), $"{action} called the service without a user id.");
        }



        [Fact]
        public async Task DeleteSet_SomeoneElsesSet_LetsNotFoundBubbleToMiddleware()
        {
            _sets.DeleteSetAsync(UserId, 4).ThrowsAsync(new NotFoundException("Set not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.DeleteSet(4));
        }

        [Fact]
        public async Task DeleteCard_CardFromDifferentSet_LetsNotFoundBubbleToMiddleware()
        {
            _sets.DeleteCardAsync(UserId, 4, 9).ThrowsAsync(new NotFoundException("Card not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.DeleteCard(4, 9));
        }

        [Fact]
        public async Task GetCards_SomeoneElsesSet_LetsNotFoundBubbleToMiddleware()
        {
            _sets.GetCardsAsync(UserId, 4).ThrowsAsync(new NotFoundException("Set not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetCards(4));
        }

        [Fact]
        public async Task AddCard_DuplicateTerm_LetsValidationBubbleToMiddleware()
        {
            _sets.AddCardAsync(UserId, 4, "labas", "hello").ThrowsAsync(new ValidationException("Term already exists."));

            await Assert.ThrowsAsync<ValidationException>(() => _controller.AddCard(4, new CreateCard("labas", "hello")));
        }



        [Theory]
        [InlineData(nameof(SetsController.GetAllSets), "GET", "api/sets")]
        [InlineData(nameof(SetsController.GetSet), "GET", "api/sets/{setId}")]
        [InlineData(nameof(SetsController.AddSet), "POST", "api/sets")]
        [InlineData(nameof(SetsController.DeleteSet), "DELETE", "api/sets/{setId}")]
        [InlineData(nameof(SetsController.DeleteCard), "DELETE", "api/sets/{setId}/cards/{cardId}")]
        [InlineData(nameof(SetsController.AddCard), "POST", "api/sets/{setId}/cards")]
        [InlineData(nameof(SetsController.GetCards), "GET", "api/sets/{setId}/cards")]
        [InlineData(nameof(SetsController.DownloadSet), "GET", "api/sets/{setId}/download/{fileType}")]
        public void RouteAttributes_EachAction_MapToExpectedVerbAndRoute(string action, string verb, string route)
        {
            Assert.Equal((verb, route), ControllerTestHelpers.RouteOf<SetsController>(action));
        }

        [Theory]
        [InlineData(nameof(SetsController.GetAllSets))]
        [InlineData(nameof(SetsController.GetSet))]
        [InlineData(nameof(SetsController.AddSet))]
        [InlineData(nameof(SetsController.DeleteSet))]
        [InlineData(nameof(SetsController.DeleteCard))]
        [InlineData(nameof(SetsController.AddCard))]
        [InlineData(nameof(SetsController.GetCards))]
        [InlineData(nameof(SetsController.DownloadSet))]
        public void AuthorizeAttributes_EachAction_RequireLogin(string action)
        {
            Assert.True(ControllerTestHelpers.RequiresAuthorization<SetsController>(action));
        }
    }
}
