using QuattroLingo.Interfaces.Services;
using QuattroLingo.Services;

namespace QuattroLingo.BackgroundServices
{
    public class RoomCleanupService(
        IGameService games,
        GameOptions options,
        TimeProvider time,
        ILogger<RoomCleanupService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(options.CleanupInterval, time);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await games.CleanupAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // One bad pass must not kill the job.
                        logger.LogError(ex, "Room cleanup pass failed.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }
    }
}