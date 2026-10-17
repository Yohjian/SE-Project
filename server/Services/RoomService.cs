using QuattroLingo.DTOs.Request;
using RoomResponse = QuattroLingo.DTOs.Response.Room;
using QuattroLingo.Repositories;
using System.Security.Cryptography;
using QuattroLingo.Entities;
using QuattroLingo.Enums;

namespace QuattroLingo.Services
{
    public class RoomService : IRoomService
    {
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
            var quiz = await _quizRepository.GetByIdAsync(dto.QuizId);

            if (quiz == null)
            {
                throw new KeyNotFoundException("Quiz not found.");
            }

            if (quiz.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You can only create rooms for your own quizzes.");
            }

            string joinCode;

            do
            {
                joinCode = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();
            }
            while (await _roomRepository.GetByJoinCodeAsync(joinCode) != null);

            var room = new Room
            {
                QuizId = dto.QuizId,
                HostId = userId,
                JoinCode = joinCode,
                RoomVisibility = dto.RoomVisibility,
                Status = RoomStatus.Waiting,
                CreatedAt = DateTime.UtcNow
            };

            var createdRoom = await _roomRepository.CreateRoomAsync(room);

            return new RoomResponse
            {
                Id = createdRoom.Id,
                QuizId = createdRoom.QuizId,
                QuizTitle = quiz.Title,
                JoinCode = createdRoom.JoinCode,
                Visibility = createdRoom.RoomVisibility,
                Status = createdRoom.Status,
                ParticipantCount = 0
            };
        }

        public async Task<List<RoomResponse>> GetPublicRoomAsync()
        {
            var rooms = await _roomRepository.GetPublicWaitingRoomsAsync();

            return rooms.Select(room => new RoomResponse
            {
                Id = room.Id,
                QuizId = room.QuizId,
                QuizTitle = room.Quiz?.Title ?? "Unknown Quiz",
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count
            }).ToList();
        }

        public async Task<RoomResponse?> GetByCodeAsync(string code)
        {
            // Patikriname, ar kodas įvestas
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var room = await _roomRepository.GetByJoinCodeAsync(code);

            if (room == null)
            {
                return null;
            }

            return new RoomResponse
            {
                Id = room.Id,
                QuizId = room.QuizId,
                QuizTitle = room.Quiz?.Title ?? "Unknown Quiz",
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count
            };
        }

        public async Task<RoomResponse> JoinRoomAsync(JoinRoom dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JoinCode))
            {
                throw new ArgumentException("Join code is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                throw new ArgumentException("Display name is required.");
            }

            var room = await _roomRepository.GetByJoinCodeAsync(dto.JoinCode.Trim());

            if (room == null)
            {
                throw new KeyNotFoundException("Room not found.");
            }

            if (room.Status != RoomStatus.Waiting)
            {
                throw new InvalidOperationException("This room is not accepting participants.");
            }

            var participant = new Participant
            {
                RoomId = room.Id,
                DisplayName = dto.DisplayName.Trim(),
                UserId = null,
                Score = 0
            };

            await _roomRepository.AddParticipantAsync(participant);

            return new RoomResponse
            {
                Id = room.Id,
                QuizId = room.QuizId,
                QuizTitle = room.Quiz?.Title ?? "Unknown Quiz",
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count + 1
            };
        }

        public async Task<List<RoomResponse>> GetMyRoomsAsync(string userId)
        {
            var rooms = await _roomRepository.GetRoomsByHostIdAsync(userId);

            return rooms.Select(room => new RoomResponse
            {
                Id = room.Id,
                QuizId = room.QuizId,
                QuizTitle = room.Quiz?.Title ?? "Unknown Quiz",
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count
            }).ToList();
        }
    }
}