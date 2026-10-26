using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using QuattroLingo.DTOs.Response.Game;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Hubs
{
    public class GameHub(IGameService games) : Hub
    {
        public Task<RoomSnapshot> JoinRoom(string joinCode, string? playerToken) =>
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