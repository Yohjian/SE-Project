using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using QuattroLingo.DTOs.Response.Game;
using QuattroLingo.Helpers;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Hubs
{
    public class GameHub(IGameService games, IOptions<RateLimitOptions> rateLimits) : Hub
    {
        private const string JoinAttemptsKey = "joinAttempts";

        public Task<RoomSnapshot> JoinRoom(string joinCode, string? playerToken)
        {
            // Limits join-code guessing through one long-lived connection (the HTTP rate
            // limiter only sees the connection being opened). A reconnect is a new connection.
            var limits = rateLimits.Value;
            var attempts = (int)(Context.Items.TryGetValue(JoinAttemptsKey, out var n) ? n! : 0) + 1;
            Context.Items[JoinAttemptsKey] = attempts;
            if (limits.Enabled && attempts > limits.HubJoinAttemptsPerConnection)
                throw new HubException("Too many join attempts. Reconnect and try again later.");

            return JoinCore(joinCode, playerToken);
        }

        private Task<RoomSnapshot> JoinCore(string joinCode, string? playerToken) =>
            games.JoinAsync(
                Context.ConnectionId,
                joinCode,
                playerToken,
                // Not Context.UserIdentifier: you set MapInboundClaims = false, so the id is "sub",
                // not ClaimTypes.NameIdentifier, and UserIdentifier would always be null.
                Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub));

        public Task StartGame() => games.StartAsync(Context.ConnectionId);

        public Task SubmitAnswer(int questionIndex, int optionId) =>
            games.SubmitAnswerAsync(Context.ConnectionId, questionIndex, optionId);

        public Task LeaveRoom() => games.LeaveAsync(Context.ConnectionId);

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await games.DisconnectAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}