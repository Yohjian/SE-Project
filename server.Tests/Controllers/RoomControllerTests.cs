using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuattroLingo.Controllers;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;
using RoomResponse = QuattroLingo.DTOs.Response.Room;

namespace QuattroLingo.Tests.Controllers
{
    public class RoomControllerTests
    {
        private const string UserId = "user-1";

        private readonly IRoomService _rooms = Substitute.For<IRoomService>();
        private readonly IGameService _games = Substitute.For<IGameService>();
        private readonly RoomController _controller;

        public RoomControllerTests()
        {
            _controller = new RoomController(_rooms, _games).WithUser(UserId);
        }

        private static RoomResponse MakeRoom(string code = "ABC123") => new()
        {
            Id = 1,
            QuizId = 7,
            QuizTitle = "Lithuanian basics",
            JoinCode = code,
            Visibility = Visibility.Public,
            Status = RoomStatus.Waiting
        };

        // ---- CreateRoom ----

        [Fact]
        public async Task CreateRoom_SignedInUser_PassesDtoAndUserIdToService()
        {
            var dto = new CreateRoom { QuizId = 7, RoomVisibility = Visibility.Private };
            _rooms.CreateRoomAsync(dto, UserId).Returns(MakeRoom());

            await _controller.CreateRoom(dto);

            await _rooms.Received(1).CreateRoomAsync(dto, UserId);
        }

        [Fact]
        public async Task CreateRoom_SignedInUser_ReturnsCreatedAtGetByCodeWithJoinCode()
        {
            var room = MakeRoom("XYZ789");
            _rooms.CreateRoomAsync(Arg.Any<CreateRoom>(), UserId).Returns(room);

            var result = await _controller.CreateRoom(new CreateRoom { QuizId = 7 });

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(RoomController.GetByCode), created.ActionName);
            Assert.Equal("XYZ789", created.RouteValues!["code"]);
            Assert.Same(room, created.Value);
        }

        [Fact]
        public async Task CreateRoom_TokenWithoutUserId_ThrowsUnauthorizedAndCallsNoService()
        {
            _controller.WithUser(userId: null);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _controller.CreateRoom(new CreateRoom()));
            await _rooms.DidNotReceiveWithAnyArgs().CreateRoomAsync(default!, default!);
        }

        // ---- GetPublicRooms ----

        [Fact]
        public async Task GetPublicRooms_AnonymousRequest_ReturnsOkWithServiceRooms()
        {
            var rooms = new List<RoomResponse> { MakeRoom("AAA111"), MakeRoom("BBB222") };
            _rooms.GetPublicRoomAsync().Returns(rooms);
            _controller.Anonymous();

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetPublicRooms());

            Assert.Same(rooms, ok.Value);
        }

        // ---- GetMyRooms ----

        [Fact]
        public async Task GetMyRooms_SignedInUser_ReturnsOkWithThatUsersRooms()
        {
            var rooms = new List<RoomResponse> { MakeRoom() };
            _rooms.GetMyRoomsAsync(UserId).Returns(rooms);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetMyRooms());

            Assert.Same(rooms, ok.Value);
        }

        [Fact]
        public async Task GetMyRooms_TokenWithoutUserId_ThrowsUnauthorized()
        {
            _controller.WithUser(userId: null);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _controller.GetMyRooms());
        }

        // ---- GetByCode ----

        [Fact]
        public async Task GetByCode_ExistingCode_ReturnsOkWithRoom()
        {
            var room = MakeRoom();
            _rooms.GetByCodeAsync("ABC123").Returns(room);
            _controller.Anonymous();

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetByCode("ABC123"));

            Assert.Same(room, ok.Value);
        }

        // ---- CancelRoom ----

        [Fact]
        public async Task CancelRoom_SignedInUser_CancelsThroughGameServiceAsHost()
        {
            await _controller.CancelRoom("ABC123");

            await _games.Received(1).CancelRoomAsync("ABC123", UserId);
        }

        [Fact]
        public async Task CancelRoom_SignedInUser_ReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.CancelRoom("ABC123"));
        }

        // ---- JoinRoom ----

        [Fact]
        public async Task JoinRoom_AnonymousPlayer_ReturnsOkWithJoinedRoomAndPlayerToken()
        {
            var dto = new JoinRoom { JoinCode = "ABC123", DisplayName = "Ona" };
            var joined = new JoinedRoom { Room = MakeRoom(), ParticipantId = 5, DisplayName = "Ona", PlayerToken = "secret" };
            _rooms.JoinRoomAsync(dto).Returns(joined);
            _controller.Anonymous();

            var ok = Assert.IsType<OkObjectResult>(await _controller.JoinRoom(dto));

            Assert.Same(joined, ok.Value);
        }

        // ---- DeleteRoom ----

        [Fact]
        public async Task DeleteRoom_SignedInUser_PassesCodeAndUserIdToService()
        {
            await _controller.DeleteRoom("ABC123");

            await _rooms.Received(1).DeleteRoomAsync("ABC123", UserId);
        }

        [Fact]
        public async Task DeleteRoom_SignedInUser_ReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.DeleteRoom("ABC123"));
        }

        // ---- Missing user id ----

        [Fact]
        public async Task CancelRoom_TokenWithoutUserId_ThrowsUnauthorizedAndCancelsNothing()
        {
            _controller.WithUser(userId: null);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _controller.CancelRoom("ABC123"));
            await _games.DidNotReceiveWithAnyArgs().CancelRoomAsync(default!, default!);
        }

        [Fact]
        public async Task DeleteRoom_TokenWithoutUserId_ThrowsUnauthorizedAndDeletesNothing()
        {
            _controller.WithUser(userId: null);

            await Assert.ThrowsAsync<UnauthorizedException>(() => _controller.DeleteRoom("ABC123"));
            await _rooms.DidNotReceiveWithAnyArgs().DeleteRoomAsync(default!, default!);
        }

        // ---- Service errors pass through to the middleware ----

        [Fact]
        public async Task CreateRoom_QuizOwnedBySomeoneElse_LetsForbiddenBubbleToMiddleware()
        {
            _rooms.CreateRoomAsync(Arg.Any<CreateRoom>(), UserId).ThrowsAsync(new ForbiddenException("Not your quiz."));

            await Assert.ThrowsAsync<ForbiddenException>(() => _controller.CreateRoom(new CreateRoom { QuizId = 7 }));
        }

        [Fact]
        public async Task GetByCode_UnknownCode_LetsNotFoundBubbleToMiddleware()
        {
            _rooms.GetByCodeAsync("ZZZ999").ThrowsAsync(new NotFoundException("Room not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetByCode("ZZZ999"));
        }

        [Fact]
        public async Task CancelRoom_NotTheHost_LetsForbiddenBubbleToMiddleware()
        {
            _games.CancelRoomAsync("ABC123", UserId).ThrowsAsync(new ForbiddenException("Only the host can close the room."));

            await Assert.ThrowsAsync<ForbiddenException>(() => _controller.CancelRoom("ABC123"));
        }

        [Fact]
        public async Task CancelRoom_GameAlreadyStarted_LetsConflictBubbleToMiddleware()
        {
            _games.CancelRoomAsync("ABC123", UserId).ThrowsAsync(new ConflictException("The game has already started."));

            await Assert.ThrowsAsync<ConflictException>(() => _controller.CancelRoom("ABC123"));
        }

        [Fact]
        public async Task JoinRoom_RoomFull_LetsConflictBubbleToMiddleware()
        {
            var dto = new JoinRoom { JoinCode = "ABC123", DisplayName = "Ona" };
            _rooms.JoinRoomAsync(dto).ThrowsAsync(new ConflictException("Room is full."));
            _controller.Anonymous();

            await Assert.ThrowsAsync<ConflictException>(() => _controller.JoinRoom(dto));
        }

        [Fact]
        public async Task DeleteRoom_RoomStillActive_LetsConflictBubbleToMiddleware()
        {
            _rooms.DeleteRoomAsync("ABC123", UserId).ThrowsAsync(new ConflictException("Room is still active."));

            await Assert.ThrowsAsync<ConflictException>(() => _controller.DeleteRoom("ABC123"));
        }

        // ---- Routing & authorization ----

        [Theory]
        [InlineData(nameof(RoomController.CreateRoom), "POST", "api/rooms")]
        [InlineData(nameof(RoomController.GetPublicRooms), "GET", "api/rooms/public")]
        [InlineData(nameof(RoomController.GetMyRooms), "GET", "api/rooms/mine")]
        [InlineData(nameof(RoomController.GetByCode), "GET", "api/rooms/{code}")]
        [InlineData(nameof(RoomController.CancelRoom), "POST", "api/rooms/{code}/cancel")]
        [InlineData(nameof(RoomController.JoinRoom), "POST", "api/rooms/join")]
        [InlineData(nameof(RoomController.DeleteRoom), "DELETE", "api/rooms/{code}")]
        public void RouteAttributes_EachAction_MapToExpectedVerbAndRoute(string action, string verb, string route)
        {
            Assert.Equal((verb, route), ControllerTestHelpers.RouteOf<RoomController>(action));
        }

        [Theory]
        [InlineData(nameof(RoomController.CreateRoom), true)]
        [InlineData(nameof(RoomController.GetPublicRooms), false)]
        [InlineData(nameof(RoomController.GetMyRooms), true)]
        [InlineData(nameof(RoomController.GetByCode), false)]
        [InlineData(nameof(RoomController.CancelRoom), true)]
        [InlineData(nameof(RoomController.JoinRoom), false)]   // guests join without an account
        [InlineData(nameof(RoomController.DeleteRoom), true)]
        public void AuthorizeAttributes_EachAction_RequireLoginOnlyWhereExpected(string action, bool requiresLogin)
        {
            Assert.Equal(requiresLogin, ControllerTestHelpers.RequiresAuthorization<RoomController>(action));
        }
    }
}
