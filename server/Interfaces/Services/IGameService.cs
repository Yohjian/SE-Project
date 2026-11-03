using QuattroLingo.DTOs.Response.Game;

namespace QuattroLingo.Interfaces.Services
{
    public interface IGameService
    {
        Task<RoomSnapshot> JoinAsync(string connectionId, string joinCode, string? playerToken, string? hostUserId);
        Task StartAsync(string connectionId);
        Task SubmitAnswerAsync(string connectionId, int questionIndex, int optionId);
        Task LeaveAsync(string connectionId);
        Task DisconnectAsync(string connectionId);

        /// <summary>Housekeeping pass, called periodically by a background service.</summary>
        Task CleanupAsync(CancellationToken ct = default);

        /// <summary>Live games die with the process, so rooms left InProgress are cancelled on startup.</summary>
        Task ReconcileOnStartupAsync();
    }
}