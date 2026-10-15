using QuattroLingo.DTOs;
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

        public async Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto, string userId)
        {
            // 1. Surandame pasirinktą viktoriną
            var quiz = await _quizRepository.GetByIdAsync(dto.QuizId);

            if (quiz == null)
            {
                throw new KeyNotFoundException("Quiz not found.");
            }

            // 2. Patikriname, ar vartotojas yra viktorinos savininkas
            if (quiz.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You can only create rooms for your own quizzes.");
            }

            // 3. Sugeneruojame unikalų kambario kodą
            string joinCode;

            do
            {
                joinCode = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();
            }
            while (await _roomRepository.GetByJoinCodeAsync(joinCode) != null);

            // 4. Sukuriame kambarį
            var room = new Room
            {
                QuizId = dto.QuizId,
                HostId = userId,
                JoinCode = joinCode,
                RoomVisibility = dto.RoomVisibility,
                Status = RoomStatus.Waiting,
                CreatedAt = DateTime.UtcNow
            };

            // 5. Išsaugome duomenų bazėje
            var createdRoom = await _roomRepository.CreateRoomAsync(room);

            // 6. Grąžiname DTO, o ne Entity
            return new RoomResponseDto
            {
                Id = createdRoom.Id,
                QuizId = createdRoom.QuizId,
                JoinCode = createdRoom.JoinCode,
                Visibility = createdRoom.RoomVisibility,
                Status = createdRoom.Status,
                ParticipantCount = 0
            };
        }

        public async Task<List<RoomResponseDto>> GetPublicRoomAsync()
        {
            // Gauname viešus kambarius, kurių būsena Waiting
            var rooms = await _roomRepository.GetPublicWaitingRoomsAsync();

            // Kiekvieną Room entity paverčiame į RoomResponseDto
            return rooms.Select(room => new RoomResponseDto
            {
                Id = room.Id,
                QuizId = room.QuizId,
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count
            }).ToList();
        }

        public async Task<RoomResponseDto?> GetByCodeAsync(string code)
        {
            // Patikriname, ar kodas įvestas
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            // Ieškome kambario pagal prisijungimo kodą
            var room = await _roomRepository.GetByJoinCodeAsync(code);

            // Jeigu kambario nėra, grąžiname null
            if (room == null)
            {
                return null;
            }

            // Grąžiname DTO, o ne Room entity
            return new RoomResponseDto
            {
                Id = room.Id,
                QuizId = room.QuizId,
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count
            };
        }

        public async Task<RoomResponseDto> JoinRoomAsync(JoinRoomDto dto)
        {
            // 1. Patikriname įvestus duomenis
            if (string.IsNullOrWhiteSpace(dto.JoinCode))
            {
                throw new ArgumentException("Join code is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                throw new ArgumentException("Display name is required.");
            }

            // 2. Surandame kambarį pagal kodą
            var room = await _roomRepository.GetByJoinCodeAsync(dto.JoinCode.Trim());

            if (room == null)
            {
                throw new KeyNotFoundException("Room not found.");
            }

            // 3. Leidžiame prisijungti tik prie laukiančio kambario
            if (room.Status != RoomStatus.Waiting)
            {
                throw new InvalidOperationException("This room is not accepting participants.");
            }

            // 4. Sukuriame naują dalyvį
            var participant = new Participant
            {
                RoomId = room.Id,
                DisplayName = dto.DisplayName.Trim(),
                UserId = null,
                Score = 0
            };

            // 5. Išsaugome dalyvį
            await _roomRepository.AddParticipantAsync(participant);

            // 6. Grąžiname atnaujintą kambario informaciją
            return new RoomResponseDto
            {
                Id = room.Id,
                QuizId = room.QuizId,
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count + 1
            };
        }

        public async Task<List<RoomResponseDto>> GetMyRoomsAsync(string userId)
        {
            var rooms = await _roomRepository.GetRoomsByHostIdAsync(userId);

            return rooms.Select(room => new RoomResponseDto
            {
                Id = room.Id,
                QuizId = room.QuizId,
                JoinCode = room.JoinCode,
                Visibility = room.RoomVisibility,
                Status = room.Status,
                ParticipantCount = room.Participants.Count
            }).ToList();
        }
    }
}