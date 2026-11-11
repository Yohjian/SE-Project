using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QuattroLingo.BackgroundServices;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.BackgroundServices
{
    public class RoomCleanupServiceTests
    {
        private readonly IGameService _games = Substitute.For<IGameService>();
        private readonly FakeTimeProvider _time = new();
        private readonly GameOptions _options = new();
        private int _calls;

        public RoomCleanupServiceTests()
        {
            _games.CleanupAsync(Arg.Any<CancellationToken>())
                .Returns(_ => { Interlocked.Increment(ref _calls); return Task.CompletedTask; });
        }

        private RoomCleanupService CreateService(ILogger<RoomCleanupService>? logger = null) =>
            new(_games, _options, _time, logger ?? NullLogger<RoomCleanupService>.Instance);

        /// <summary>
        /// The timer may not exist yet when StartAsync returns, so keep advancing the
        /// fake clock until the expected number of cleanup passes has happened.
        /// </summary>
        private async Task TickUntilAsync(int expectedCalls)
        {
            for (var i = 0; i < 200 && Volatile.Read(ref _calls) < expectedCalls; i++)
            {
                _time.Advance(_options.CleanupInterval);
                await Task.Delay(10);
            }
        }



        [Fact]
        public async Task ExecuteAsync_BeforeFirstInterval_DoesNotRunCleanup()
        {
            var service = CreateService();

            await service.StartAsync(CancellationToken.None);
            _time.Advance(_options.CleanupInterval - TimeSpan.FromSeconds(1));
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);

            Assert.Equal(0, Volatile.Read(ref _calls));
        }

        [Fact]
        public async Task ExecuteAsync_SeveralIntervalsPass_RunsCleanupOncePerInterval()
        {
            var service = CreateService();

            await service.StartAsync(CancellationToken.None);
            await TickUntilAsync(3);
            await service.StopAsync(CancellationToken.None);

            Assert.True(Volatile.Read(ref _calls) >= 3);
        }

        [Fact]
        public async Task ExecuteAsync_CleanupPassThrows_KeepsRunningLaterPasses()
        {
            var calls = 0;
            _games.CleanupAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            {
                var n = Interlocked.Increment(ref calls);
                Volatile.Write(ref _calls, n);
                return n == 1 ? Task.FromException(new InvalidOperationException("boom")) : Task.CompletedTask;
            });
            var service = CreateService();

            await service.StartAsync(CancellationToken.None);
            await TickUntilAsync(2);
            await service.StopAsync(CancellationToken.None);

            Assert.True(Volatile.Read(ref calls) >= 2);
        }

        [Fact]
        public async Task ExecuteAsync_CleanupPassThrows_LogsError()
        {
            var logger = Substitute.For<ILogger<RoomCleanupService>>();
            var failure = new InvalidOperationException("boom");
            _games.CleanupAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            {
                Interlocked.Increment(ref _calls);
                return Task.FromException(failure);
            });
            var service = CreateService(logger);

            await service.StartAsync(CancellationToken.None);
            await TickUntilAsync(1);
            await service.StopAsync(CancellationToken.None);

            logger.Received().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                Arg.Is<Exception?>(e => e == failure),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>());
        }

        [Fact]
        public async Task ExecuteAsync_ServiceStopped_PassesCancelledStoppingTokenToCleanup()
        {
            CancellationToken seen = default;
            _games.CleanupAsync(Arg.Any<CancellationToken>()).Returns(ci =>
            {
                seen = ci.Arg<CancellationToken>();
                Interlocked.Increment(ref _calls);
                return Task.CompletedTask;
            });
            var service = CreateService();

            await service.StartAsync(CancellationToken.None);
            await TickUntilAsync(1);
            await service.StopAsync(CancellationToken.None);

            Assert.True(seen.CanBeCanceled);
            Assert.True(seen.IsCancellationRequested);
        }

        [Fact]
        public async Task ExecuteAsync_ServiceStopped_StopsRunningCleanup()
        {
            var service = CreateService();
            await service.StartAsync(CancellationToken.None);
            await TickUntilAsync(1);

            await service.StopAsync(CancellationToken.None);
            var callsAtStop = Volatile.Read(ref _calls);
            _time.Advance(_options.CleanupInterval * 5);
            await Task.Delay(50);

            Assert.Equal(callsAtStop, Volatile.Read(ref _calls));
        }

        [Fact]
        public async Task ExecuteAsync_CleanupCancelledDuringShutdown_StopsWithoutThrowing()
        {
            _games.CleanupAsync(Arg.Any<CancellationToken>()).Returns(ci =>
            {
                Interlocked.Increment(ref _calls);
                return Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>());
            });
            var service = CreateService();
            await service.StartAsync(CancellationToken.None);
            await TickUntilAsync(1);

            var ex = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));

            Assert.Null(ex);
            Assert.True(service.ExecuteTask!.IsCompletedSuccessfully);
        }
    }
}
