using System.Security.Cryptography;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Extensions;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.Helpers;
using RoomResponse = QuattroLingo.DTOs.Response.Room;
using RoomEntity = QuattroLingo.Entities.Room;

namespace QuattroLingo.Services
{
    public class RoomService : IRoomService
    {
        public const int MaxDisplayNameLength = 30;
        public const int MaxParticipants = 50;

        private const int JoinCodeLength = 6;
        private const int MaxCodeAttempts = 5;
        private const string RoomNotFound = "Room not found.";

        private readonly IRoomRepository _roomRepository;
        private readonly IQuizRepository _quizRepository;

        public RoomService(IRoomRepository roomRepository,
                           IQuizRepository quizRepository)
        {
            _roomRepository = roomRepository;
            _quizRepository = quizRepository;
        }

        public async Task<RoomResponse> CreateRoomAsync(CreateRoom dto, string userId)
        {
            if (!Enum.IsDefined(dto.RoomVisibility))
            {
                throw new ValidationException("Invalid room visibility.");
            }

            var quiz = await _quizRepository.GetByIdAsync(dto.QuizId)
                ?? throw new NotFoundException("Quiz not found.");

            if (quiz.OwnerId != userId)
            {
                throw new ForbiddenException("You can only create rooms for your own quizzes.");
            }

            for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
            {
                var room = new RoomEntity
                {
                    QuizId = quiz.Id,
                    Quiz = quiz,
                    HostId = userId,
                    JoinCode = GenerateJoinCode(),
                    RoomVisibility = dto.RoomVisibility,
                    Status = RoomStatus.Waiting,
                    CreatedAt = DateTime.UtcNow
                };

                if (await _roomRepository.TryCreateRoomAsync(room))
                {
                    return room.ToResponse();
                }
            }

            throw new InvalidOperationException("Could not generate a unique join code.");
        }

        public async Task<List<RoomResponse>> GetPublicRoomAsync()
        {
            var rooms = await _roomRepository.GetPublicWaitingRoomsAsync();
            return rooms.Select(room => room.ToResponse()).ToList();
        }

        public async Task<RoomResponse> GetByCodeAsync(string code)
        {
            var normalized = NormalizeCode(code) ?? throw new NotFoundException(RoomNotFound);

            var room = await _roomRepository.GetByJoinCodeAsync(normalized)
                ?? throw new NotFoundException(RoomNotFound);

            return room.ToResponse();
        }

        public async Task<JoinedRoom> JoinRoomAsync(JoinRoom dto)
        {
            var code = NormalizeCode(dto.JoinCode)
                ?? throw new NotFoundException(RoomNotFound);

            var name = dto.DisplayName?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                throw new ValidationException("Display name is required.");
            }

            if (name.Length > MaxDisplayNameLength)
            {
                throw new ValidationException(
                    $"Display name can be at most {MaxDisplayNameLength} characters.");
            }

            var room = await _roomRepository.GetByJoinCodeAsync(code)
                ?? throw new NotFoundException(RoomNotFound);

            if (room.Status != RoomStatus.Waiting)
            {
                throw new ConflictException("This room is not accepting participants.");
            }

            if (room.Participants.Count >= MaxParticipants)
            {
                throw new ConflictException("This room is full.");
            }

            if (room.Participants.Any(p =>
                    string.Equals(p.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("That name is already taken in this room.");
            }

            // Read the count before adding the participant.
            var countBefore = room.Participants.Count;

            var (token, tokenHash) = PlayerTokens.Generate();

            var participant = await _roomRepository.AddParticipantAsync(new Participant
            {
                RoomId = room.Id,
                DisplayName = name,
                UserId = null,
                Score = 0,
                PlayerTokenHash = tokenHash
            });

            return new JoinedRoom
            {
                Room = room.ToResponse(countBefore + 1),
                ParticipantId = participant.Id,
                DisplayName = participant.DisplayName,
                PlayerToken = token
            };
        }

        public async Task<List<RoomResponse>> GetMyRoomsAsync(string userId)
        {
            var rooms = await _roomRepository.GetRoomsByHostIdAsync(userId);
            return rooms.Select(room => room.ToResponse()).ToList();
        }

        private static string GenerateJoinCode() =>
            RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        private static string? NormalizeCode(string? code)
        {
            var trimmed = code?.Trim();
            return trimmed is { Length: JoinCodeLength } && trimmed.All(char.IsAsciiDigit)
                ? trimmed
                : null;
        }
    }
}