using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Extensions;

namespace QuattroLingo.Tests.Extensions
{
    public class RoomMappingExtensionsTests
    {
        private static Room MakeRoom(int participants = 0, Quiz? quiz = null) => new()
        {
            Id = 11,
            QuizId = 7,
            Quiz = quiz,
            HostId = "host-1",
            JoinCode = "ABC123",
            RoomVisibility = Visibility.Private,
            Status = RoomStatus.InProgress,
            Participants = Enumerable.Range(1, participants)
                .Select(i => new Participant { Id = i, DisplayName = $"Player {i}" })
                .ToList()
        };



        [Fact]
        public void ToResponse_AnyRoom_CopiesIdQuizIdJoinCodeVisibilityAndStatus()
        {
            var response = MakeRoom().ToResponse();

            Assert.Equal(11, response.Id);
            Assert.Equal(7, response.QuizId);
            Assert.Equal("ABC123", response.JoinCode);
            Assert.Equal(Visibility.Private, response.Visibility);
            Assert.Equal(RoomStatus.InProgress, response.Status);
        }

        [Fact]
        public void ToResponse_QuizLoaded_UsesQuizTitle()
        {
            var quiz = new Quiz { Id = 7, OwnerId = "host-1", Title = "Lithuanian basics" };

            Assert.Equal("Lithuanian basics", MakeRoom(quiz: quiz).ToResponse().QuizTitle);
        }

        [Fact]
        public void ToResponse_QuizNotLoaded_ReturnsUnknownQuizTitle()
        {
            Assert.Equal("Unknown Quiz", MakeRoom(quiz: null).ToResponse().QuizTitle);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void ToResponse_NoCountGiven_CountsLoadedParticipants(int participants)
        {
            Assert.Equal(participants, MakeRoom(participants).ToResponse().ParticipantCount);
        }

        [Fact]
        public void ToResponse_CountGiven_UsesGivenCountInsteadOfLoadedParticipants()
        {
            var room = MakeRoom(participants: 0);   // participants not loaded, count queried separately

            Assert.Equal(5, room.ToResponse(participantCount: 5).ParticipantCount);
        }

        [Fact]
        public void ToResponse_CountOfZeroGiven_ReturnsZeroEvenWithLoadedParticipants()
        {
            Assert.Equal(0, MakeRoom(participants: 2).ToResponse(participantCount: 0).ParticipantCount);
        }
    }
}
