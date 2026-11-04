using NSubstitute;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services
{
    public class QuizServiceActiveRoomTests
    {
        private static Quiz OwnedQuiz() => new() { Id = 1, OwnerId = "user-1", Title = "Quiz" };

        [Fact]
        public async Task Delete_QuizWithActiveRoom_ThrowsConflictAndDeletesNothing()
        {
            var repo = Substitute.For<IQuizRepository>();
            var quiz = OwnedQuiz();
            repo.GetByIdAsync(1).Returns(quiz);
            repo.HasActiveRoomAsync(1).Returns(true);

            await Assert.ThrowsAsync<ConflictException>(
                () => new QuizService(repo).DeleteAsync("user-1", 1));

            await repo.DidNotReceive().DeleteAsync(Arg.Any<Quiz>());
        }

        [Fact]
        public async Task Delete_QuizWithoutActiveRoom_Succeeds()
        {
            var repo = Substitute.For<IQuizRepository>();
            var quiz = OwnedQuiz();
            repo.GetByIdAsync(1).Returns(quiz);
            repo.HasActiveRoomAsync(1).Returns(false);

            await new QuizService(repo).DeleteAsync("user-1", 1);

            await repo.Received().DeleteAsync(quiz);
        }

        [Fact]
        public async Task Delete_ByNonOwner_IsForbiddenEvenWhenRoomIsActive()
        {
            var repo = Substitute.For<IQuizRepository>();
            repo.GetByIdAsync(1).Returns(OwnedQuiz());
            repo.HasActiveRoomAsync(1).Returns(true);

            await Assert.ThrowsAsync<ForbiddenException>(
                () => new QuizService(repo).DeleteAsync("someone-else", 1));
        }

        [Fact]
        public async Task DeleteQuestion_QuizWithActiveRoom_ThrowsConflict()
        {
            var repo = Substitute.For<IQuizRepository>();
            var quiz = OwnedQuiz();
            var question = new Question { Id = 5, QuizId = 1, Quiz = quiz, Text = "Q" };
            repo.GetQuestionByIdAsync(5).Returns(question);
            repo.HasActiveRoomAsync(1).Returns(true);

            await Assert.ThrowsAsync<ConflictException>(
                () => new QuizService(repo).DeleteQuestionAsync("user-1", 5));

            await repo.DidNotReceive().DeleteQuestionAsync(Arg.Any<Question>());
        }

        [Fact]
        public async Task DeleteAnswerOption_QuizWithActiveRoom_ThrowsConflict()
        {
            var repo = Substitute.For<IQuizRepository>();
            var quiz = OwnedQuiz();
            var question = new Question { Id = 5, QuizId = 1, Quiz = quiz, Text = "Q" };
            var option = new AnswerOption { Id = 9, QuestionId = 5, Question = question, Text = "A" };
            repo.GetAnswerOptionByIdAsync(9).Returns(option);
            repo.HasActiveRoomAsync(1).Returns(true);

            await Assert.ThrowsAsync<ConflictException>(
                () => new QuizService(repo).DeleteAnswerOptionAsync("user-1", 9));
        }
    }
}