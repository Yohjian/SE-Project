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
        [Fact]
        public async Task RunsCleanupOnEveryTick_AndSurvivesFailures()
        {
            var games = Substitute.For<IGameService>();
            var calls = 0;
            games.CleanupAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            {
                calls++;
                return calls == 1 ? Task.FromException(new InvalidOperationException("boom")) : Task.CompletedTask;
            });

            var time = new FakeTimeProvider();
            var options = new GameOptions();
            var service = new RoomCleanupService(games, options, time, NullLogger<RoomCleanupService>.Instance);

            await service.StartAsync(CancellationToken.None);

            // The timer may not exist yet when StartAsync returns, so keep ticking
            // the clock until the expected number of cleanup passes has happened.
            for (var pass = 1; pass <= 2; pass++)
            {
                for (var i = 0; i < 200 && Volatile.Read(ref calls) < pass; i++)
                {
                    time.Advance(options.CleanupInterval);
                    await Task.Delay(10);
                }
            }
            await service.StopAsync(CancellationToken.None);

            // The first pass threw, yet the job kept going and ran a second one.
            Assert.True(calls >= 2);
        }
    }
}