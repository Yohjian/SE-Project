using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuattroLingo.Exceptions;
using QuattroLingo.Hubs;

namespace QuattroLingo.Tests.Hubs
{
    public class ExceptionHubFilterTests
    {
        private readonly ILogger<ExceptionHubFilter> _logger = Substitute.For<ILogger<ExceptionHubFilter>>();
        private readonly ExceptionHubFilter _filter;

        public ExceptionHubFilterTests()
        {
            _filter = new ExceptionHubFilter(_logger);
        }

        private static HubInvocationContext MakeContext() => new(
            Substitute.For<HubCallerContext>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<Hub>(),
            typeof(GameHub).GetMethod(nameof(GameHub.SubmitAnswer))!,
            [0, 1]);

        private static Func<HubInvocationContext, ValueTask<object?>> Throwing(Exception ex) =>
            _ => throw ex;

        public static TheoryData<Exception> DomainExceptions => new()
        {
            new ValidationException("Option does not belong to this question."),
            new UnauthorizedException("Invalid player token."),
            new ForbiddenException("Only the host can start the game."),
            new NotFoundException("Room not found."),
            new ConflictException("You already answered this question.")
        };



        [Fact]
        public async Task InvokeMethodAsync_HubMethodSucceeds_ReturnsItsResult()
        {
            var result = await _filter.InvokeMethodAsync(MakeContext(), _ => ValueTask.FromResult<object?>("ok"));

            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task InvokeMethodAsync_HubMethodSucceeds_PassesTheSameContextToNext()
        {
            var context = MakeContext();
            HubInvocationContext? seen = null;

            await _filter.InvokeMethodAsync(context, c => { seen = c; return ValueTask.FromResult<object?>(null); });

            Assert.Same(context, seen);
        }

        [Theory]
        [MemberData(nameof(DomainExceptions))]
        public async Task InvokeMethodAsync_DomainExceptionThrown_RethrowsAsHubExceptionWithSameMessage(Exception thrown)
        {
            var ex = await Assert.ThrowsAsync<HubException>(
                () => _filter.InvokeMethodAsync(MakeContext(), Throwing(thrown)).AsTask());

            Assert.Equal(thrown.Message, ex.Message);
        }

        [Theory]
        [MemberData(nameof(DomainExceptions))]
        public async Task InvokeMethodAsync_DomainExceptionThrown_LogsWarning(Exception thrown)
        {
            await Assert.ThrowsAsync<HubException>(
                () => _filter.InvokeMethodAsync(MakeContext(), Throwing(thrown)).AsTask());

            _logger.Received(1).Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>());
        }

        [Fact]
        public async Task InvokeMethodAsync_UnexpectedExceptionThrown_RethrowsOriginalSoMessageIsNotSentToClient()
        {
            var thrown = new InvalidOperationException("connection string: Host=db;Password=secret");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _filter.InvokeMethodAsync(MakeContext(), Throwing(thrown)).AsTask());

            Assert.Same(thrown, ex);
        }

        [Fact]
        public async Task InvokeMethodAsync_UnexpectedExceptionThrown_DoesNotLogWarning()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _filter.InvokeMethodAsync(MakeContext(), Throwing(new InvalidOperationException())).AsTask());

            _logger.DidNotReceive().Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>());
        }
    }
}
