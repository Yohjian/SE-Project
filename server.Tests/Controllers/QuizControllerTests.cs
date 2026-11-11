using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuattroLingo.Controllers;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Tests.Controllers
{
    public class QuizControllerTests
    {
        private const string UserId = "user-1";

        private readonly IQuizService _quizzes = Substitute.For<IQuizService>();
        private readonly UserManager<ApplicationUser> _users;
        private readonly QuizController _controller;

        public QuizControllerTests()
        {
            _users = Substitute.For<UserManager<ApplicationUser>>(
                Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _users.FindByIdAsync(UserId).Returns(new ApplicationUser { Id = UserId, IsActive = true });

            _controller = new QuizController(_quizzes, _users).WithUser(UserId);
        }

        private static QuizEditorResponse MakeEditor(int id = 7, bool isPublic = false) =>
            new(id, "Lithuanian basics", "Greetings", [], isPublic);

        private static QuestionRequest MakeQuestionRequest() =>
            new("Labas means?", 20, 1000, 0, [new("Hello", true), new("Bye", false)]);

        private static QuestionResponse MakeQuestionResponse(int id = 3) =>
            new(id, "Labas means?", 20, 1000, 0, [new(1, "Hello", true), new(2, "Bye", false)]);

        private static CreateQuizRequest MakeCreateRequest() =>
            new("Lithuanian basics", "Greetings", [MakeQuestionRequest()]);

        // ---- GetMine ----

        [Fact]
        public async Task GetMine_SignedInUser_ReturnsOkWithThatUsersQuizzes()
        {
            var mine = new List<QuizSummary> { new(7, "Lithuanian basics", null, false, "Ona") };
            _quizzes.GetMyQuizzesAsync(UserId).Returns(mine);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetMine());

            Assert.Same(mine, ok.Value);
        }

        // ---- GetPublic / GetPublicById ----

        [Fact]
        public async Task GetPublic_AnyUser_ReturnsOkWithPublicQuizzes()
        {
            var publicQuizzes = new List<QuizSummary> { new(8, "Numbers", null, true, "Jonas") };
            _quizzes.GetPublicQuizzesAsync().Returns(publicQuizzes);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetPublic());

            Assert.Same(publicQuizzes, ok.Value);
        }

        [Fact]
        public async Task GetPublicById_PublicQuiz_ReturnsOkWithQuiz()
        {
            var quiz = MakeEditor(8, isPublic: true);
            _quizzes.GetPublicQuizByIdAsync(8).Returns(quiz);

            var ok = Assert.IsType<OkObjectResult>(await _controller.GetPublicById(8));

            Assert.Same(quiz, ok.Value);
        }

        [Fact]
        public async Task GetPublicById_PrivateOrMissingQuiz_LetsNotFoundBubbleToMiddleware()
        {
            _quizzes.GetPublicQuizByIdAsync(8).ThrowsAsync(new NotFoundException("Quiz not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetPublicById(8));
        }

        // ---- Get ----

        [Fact]
        public async Task Get_OwnQuiz_ReturnsOkWithEditorView()
        {
            var quiz = MakeEditor();
            _quizzes.GetByIdAsync(UserId, 7).Returns(quiz);

            var ok = Assert.IsType<OkObjectResult>(await _controller.Get(7));

            Assert.Same(quiz, ok.Value);
        }

        [Fact]
        public async Task Get_SomeoneElsesQuiz_LetsForbiddenBubbleToMiddleware()
        {
            _quizzes.GetByIdAsync(UserId, 7).ThrowsAsync(new ForbiddenException("Not your quiz."));

            await Assert.ThrowsAsync<ForbiddenException>(() => _controller.Get(7));
        }

        // ---- CopyPublicQuiz ----

        [Fact]
        public async Task CopyPublicQuiz_PublicQuiz_ReturnsCreatedAtGetWithNewQuizId()
        {
            var copy = MakeEditor(id: 15);
            _quizzes.CopyPublicQuizAsync(UserId, 8).Returns(copy);

            var created = Assert.IsType<CreatedAtActionResult>(await _controller.CopyPublicQuiz(8));

            Assert.Equal(nameof(QuizController.Get), created.ActionName);
            Assert.Equal(15, created.RouteValues!["id"]);
            Assert.Same(copy, created.Value);
        }

        // ---- Create ----

        [Fact]
        public async Task Create_ActiveUser_PassesUserIdAndRequestToService()
        {
            var request = MakeCreateRequest();
            _quizzes.CreateAsync(UserId, request).Returns(MakeEditor());

            await _controller.Create(request);

            await _quizzes.Received(1).CreateAsync(UserId, request);
        }

        [Fact]
        public async Task Create_ActiveUser_ReturnsCreatedAtGetWithNewQuizId()
        {
            var quiz = MakeEditor(id: 21);
            _quizzes.CreateAsync(UserId, Arg.Any<CreateQuizRequest>()).Returns(quiz);

            var created = Assert.IsType<CreatedAtActionResult>(await _controller.Create(MakeCreateRequest()));

            Assert.Equal(nameof(QuizController.Get), created.ActionName);
            Assert.Equal(21, created.RouteValues!["id"]);
            Assert.Same(quiz, created.Value);
        }

        [Fact]
        public async Task Create_TokenWithoutUserId_ReturnsUnauthorizedAndCreatesNothing()
        {
            _controller.WithUser(userId: null);

            Assert.IsType<UnauthorizedResult>(await _controller.Create(MakeCreateRequest()));
            await _quizzes.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        }

        [Fact]
        public async Task Create_UserNoLongerExists_ReturnsUnauthorizedAndCreatesNothing()
        {
            _users.FindByIdAsync(UserId).Returns((ApplicationUser?)null);

            Assert.IsType<UnauthorizedResult>(await _controller.Create(MakeCreateRequest()));
            await _quizzes.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        }

        [Fact]
        public async Task Create_DeactivatedUser_ReturnsUnauthorizedAndCreatesNothing()
        {
            _users.FindByIdAsync(UserId).Returns(new ApplicationUser { Id = UserId, IsActive = false });

            Assert.IsType<UnauthorizedResult>(await _controller.Create(MakeCreateRequest()));
            await _quizzes.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        }

        // ---- Update ----

        [Fact]
        public async Task Update_OwnQuiz_ReturnsOkWithUpdatedQuiz()
        {
            var request = new UpdateQuizRequest("New title", null, [MakeQuestionRequest()]);
            var updated = MakeEditor();
            _quizzes.UpdateAsync(UserId, 7, request).Returns(updated);

            var ok = Assert.IsType<OkObjectResult>(await _controller.Update(7, request));

            Assert.Same(updated, ok.Value);
        }

        [Fact]
        public async Task Update_QuizWithActiveRoom_LetsConflictBubbleToMiddleware()
        {
            _quizzes.UpdateAsync(UserId, 7, Arg.Any<UpdateQuizRequest>())
                .ThrowsAsync(new ConflictException("Quiz is used by an active room."));

            await Assert.ThrowsAsync<ConflictException>(
                () => _controller.Update(7, new UpdateQuizRequest("Title", null, [])));
        }

        // ---- UpdateVisibility ----

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateVisibility_OwnQuiz_PassesRequestedVisibilityAndReturnsOk(bool isPublic)
        {
            var updated = MakeEditor(isPublic: isPublic);
            _quizzes.UpdateVisibilityAsync(UserId, 7, isPublic).Returns(updated);

            var ok = Assert.IsType<OkObjectResult>(
                await _controller.UpdateVisibility(7, new UpdateQuizVisibilityRequest(isPublic)));

            Assert.Same(updated, ok.Value);
            await _quizzes.Received(1).UpdateVisibilityAsync(UserId, 7, isPublic);
        }

        // ---- Delete ----

        [Fact]
        public async Task Delete_OwnQuiz_DeletesThroughServiceAndReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.Delete(7));

            await _quizzes.Received(1).DeleteAsync(UserId, 7);
        }

        // ---- AddQuestion / UpdateQuestion / DeleteQuestion ----

        [Fact]
        public async Task AddQuestion_OwnQuiz_ReturnsOkWithCreatedQuestion()
        {
            var request = MakeQuestionRequest();
            var question = MakeQuestionResponse();
            _quizzes.AddQuestionAsync(UserId, 7, request).Returns(question);

            var ok = Assert.IsType<OkObjectResult>(await _controller.AddQuestion(7, request));

            Assert.Same(question, ok.Value);
        }

        [Fact]
        public async Task UpdateQuestion_OwnQuestion_ReturnsOkWithUpdatedQuestion()
        {
            var request = MakeQuestionRequest();
            var question = MakeQuestionResponse(3);
            _quizzes.UpdateQuestionAsync(UserId, 3, request).Returns(question);

            var ok = Assert.IsType<OkObjectResult>(await _controller.UpdateQuestion(3, request));

            Assert.Same(question, ok.Value);
        }

        [Fact]
        public async Task DeleteQuestion_OwnQuestion_DeletesThroughServiceAndReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.DeleteQuestion(3));

            await _quizzes.Received(1).DeleteQuestionAsync(UserId, 3);
        }

        // ---- AddAnswerOption / UpdateAnswerOption / DeleteAnswerOption ----

        [Fact]
        public async Task AddAnswerOption_OwnQuestion_ReturnsOkWithCreatedOption()
        {
            var request = new AnswerOptionRequest("Hi", false);
            var option = new AnswerOptionResponse(4, "Hi", false);
            _quizzes.AddAnswerOptionAsync(UserId, 3, request).Returns(option);

            var ok = Assert.IsType<OkObjectResult>(await _controller.AddAnswerOption(3, request));

            Assert.Same(option, ok.Value);
        }

        [Fact]
        public async Task UpdateAnswerOption_OwnOption_ReturnsOkWithUpdatedOption()
        {
            var request = new AnswerOptionRequest("Hello", true);
            var option = new AnswerOptionResponse(4, "Hello", true);
            _quizzes.UpdateAnswerOptionAsync(UserId, 4, request).Returns(option);

            var ok = Assert.IsType<OkObjectResult>(await _controller.UpdateAnswerOption(4, request));

            Assert.Same(option, ok.Value);
        }

        [Fact]
        public async Task DeleteAnswerOption_OwnOption_DeletesThroughServiceAndReturnsNoContent()
        {
            Assert.IsType<NoContentResult>(await _controller.DeleteAnswerOption(4));

            await _quizzes.Received(1).DeleteAnswerOptionAsync(UserId, 4);
        }

        // ---- Service errors pass through to the middleware ----

        [Fact]
        public async Task CopyPublicQuiz_PrivateOrMissingQuiz_LetsNotFoundBubbleToMiddleware()
        {
            _quizzes.CopyPublicQuizAsync(UserId, 8).ThrowsAsync(new NotFoundException("Quiz not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.CopyPublicQuiz(8));
        }

        [Fact]
        public async Task Create_InvalidQuiz_LetsValidationBubbleToMiddleware()
        {
            _quizzes.CreateAsync(UserId, Arg.Any<CreateQuizRequest>())
                .ThrowsAsync(new ValidationException("Title is required."));

            await Assert.ThrowsAsync<ValidationException>(() => _controller.Create(MakeCreateRequest()));
        }

        [Fact]
        public async Task UpdateVisibility_SomeoneElsesQuiz_LetsForbiddenBubbleToMiddleware()
        {
            _quizzes.UpdateVisibilityAsync(UserId, 7, true).ThrowsAsync(new ForbiddenException("Not your quiz."));

            await Assert.ThrowsAsync<ForbiddenException>(
                () => _controller.UpdateVisibility(7, new UpdateQuizVisibilityRequest(true)));
        }

        [Fact]
        public async Task Delete_QuizWithActiveRoom_LetsConflictBubbleToMiddleware()
        {
            _quizzes.DeleteAsync(UserId, 7).ThrowsAsync(new ConflictException("Quiz is used by an active room."));

            await Assert.ThrowsAsync<ConflictException>(() => _controller.Delete(7));
        }

        [Fact]
        public async Task AddQuestion_QuestionWithoutCorrectAnswer_LetsValidationBubbleToMiddleware()
        {
            _quizzes.AddQuestionAsync(UserId, 7, Arg.Any<QuestionRequest>())
                .ThrowsAsync(new ValidationException("Exactly one answer must be correct."));

            await Assert.ThrowsAsync<ValidationException>(() => _controller.AddQuestion(7, MakeQuestionRequest()));
        }

        [Fact]
        public async Task UpdateQuestion_UnknownQuestion_LetsNotFoundBubbleToMiddleware()
        {
            _quizzes.UpdateQuestionAsync(UserId, 3, Arg.Any<QuestionRequest>())
                .ThrowsAsync(new NotFoundException("Question not found."));

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.UpdateQuestion(3, MakeQuestionRequest()));
        }

        [Fact]
        public async Task DeleteQuestion_QuizWithActiveRoom_LetsConflictBubbleToMiddleware()
        {
            _quizzes.DeleteQuestionAsync(UserId, 3).ThrowsAsync(new ConflictException("Quiz is used by an active room."));

            await Assert.ThrowsAsync<ConflictException>(() => _controller.DeleteQuestion(3));
        }

        [Fact]
        public async Task AddAnswerOption_SomeoneElsesQuestion_LetsForbiddenBubbleToMiddleware()
        {
            _quizzes.AddAnswerOptionAsync(UserId, 3, Arg.Any<AnswerOptionRequest>())
                .ThrowsAsync(new ForbiddenException("Not your quiz."));

            await Assert.ThrowsAsync<ForbiddenException>(
                () => _controller.AddAnswerOption(3, new AnswerOptionRequest("Hi", false)));
        }

        [Fact]
        public async Task UpdateAnswerOption_UnknownOption_LetsNotFoundBubbleToMiddleware()
        {
            _quizzes.UpdateAnswerOptionAsync(UserId, 4, Arg.Any<AnswerOptionRequest>())
                .ThrowsAsync(new NotFoundException("Answer option not found."));

            await Assert.ThrowsAsync<NotFoundException>(
                () => _controller.UpdateAnswerOption(4, new AnswerOptionRequest("Hello", true)));
        }

        [Fact]
        public async Task DeleteAnswerOption_QuizWithActiveRoom_LetsConflictBubbleToMiddleware()
        {
            _quizzes.DeleteAnswerOptionAsync(UserId, 4).ThrowsAsync(new ConflictException("Quiz is used by an active room."));

            await Assert.ThrowsAsync<ConflictException>(() => _controller.DeleteAnswerOption(4));
        }

        // ---- Routing & authorization ----

        [Theory]
        [InlineData(nameof(QuizController.GetMine), "GET", "api/quizzes/mine")]
        [InlineData(nameof(QuizController.GetPublic), "GET", "api/quizzes/public")]
        [InlineData(nameof(QuizController.GetPublicById), "GET", "api/quizzes/public/{id:int}")]
        [InlineData(nameof(QuizController.Get), "GET", "api/quizzes/{id}")]
        [InlineData(nameof(QuizController.CopyPublicQuiz), "POST", "api/quizzes/{id}/copy")]
        [InlineData(nameof(QuizController.Create), "POST", "api/quizzes")]
        [InlineData(nameof(QuizController.Update), "PUT", "api/quizzes/{id}")]
        [InlineData(nameof(QuizController.UpdateVisibility), "PATCH", "api/quizzes/{id}/visibility")]
        [InlineData(nameof(QuizController.Delete), "DELETE", "api/quizzes/{id}")]
        [InlineData(nameof(QuizController.AddQuestion), "POST", "api/quizzes/{quizId}/questions")]
        [InlineData(nameof(QuizController.UpdateQuestion), "PUT", "api/quizzes/questions/{questionId}")]
        [InlineData(nameof(QuizController.DeleteQuestion), "DELETE", "api/quizzes/questions/{questionId}")]
        [InlineData(nameof(QuizController.AddAnswerOption), "POST", "api/quizzes/questions/{questionId}/answers")]
        [InlineData(nameof(QuizController.UpdateAnswerOption), "PUT", "api/quizzes/answers/{answerOptionId}")]
        [InlineData(nameof(QuizController.DeleteAnswerOption), "DELETE", "api/quizzes/answers/{answerOptionId}")]
        public void RouteAttributes_EachAction_MapToExpectedVerbAndRoute(string action, string verb, string route)
        {
            Assert.Equal((verb, route), ControllerTestHelpers.RouteOf<QuizController>(action));
        }

        [Theory]
        [InlineData(nameof(QuizController.GetMine))]
        [InlineData(nameof(QuizController.GetPublic))]
        [InlineData(nameof(QuizController.GetPublicById))]
        [InlineData(nameof(QuizController.Get))]
        [InlineData(nameof(QuizController.CopyPublicQuiz))]
        [InlineData(nameof(QuizController.Create))]
        [InlineData(nameof(QuizController.Update))]
        [InlineData(nameof(QuizController.UpdateVisibility))]
        [InlineData(nameof(QuizController.Delete))]
        [InlineData(nameof(QuizController.AddQuestion))]
        [InlineData(nameof(QuizController.UpdateQuestion))]
        [InlineData(nameof(QuizController.DeleteQuestion))]
        [InlineData(nameof(QuizController.AddAnswerOption))]
        [InlineData(nameof(QuizController.UpdateAnswerOption))]
        [InlineData(nameof(QuizController.DeleteAnswerOption))]
        public void AuthorizeAttributes_EachAction_RequireLogin(string action)
        {
            Assert.True(ControllerTestHelpers.RequiresAuthorization<QuizController>(action));
        }
    }
}
