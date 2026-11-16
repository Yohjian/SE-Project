using QuattroLingo.Entities;
using QuattroLingo.Extensions;

namespace QuattroLingo.Tests.Extensions
{
    public class QuizMappingExtensionsTests
    {
        private static Quiz MakeQuiz(ApplicationUser? owner = null) => new()
        {
            Id = 7,
            OwnerId = "user-1",
            Owner = owner,
            Title = "Lithuanian basics",
            Description = "Greetings and numbers",
            IsPublic = true
        };



        [Fact]
        public void ToSummary_AnyQuiz_CopiesIdTitleDescriptionAndVisibility()
        {
            var summary = MakeQuiz().ToSummary();

            Assert.Equal(7, summary.Id);
            Assert.Equal("Lithuanian basics", summary.Title);
            Assert.Equal("Greetings and numbers", summary.Description);
            Assert.True(summary.IsPublic);
        }

        [Fact]
        public void ToSummary_QuizWithoutDescription_ReturnsNullDescription()
        {
            var quiz = MakeQuiz();
            quiz.Description = null;

            Assert.Null(quiz.ToSummary().Description);
        }

        [Fact]
        public void ToSummary_PrivateQuiz_ReturnsIsPublicFalse()
        {
            var quiz = MakeQuiz();
            quiz.IsPublic = false;

            Assert.False(quiz.ToSummary().IsPublic);
        }

        [Fact]
        public void ToSummary_OwnerWithDisplayName_UsesDisplayNameAsCreator()
        {
            var owner = new ApplicationUser { DisplayName = "Ona", Email = "ona.p@example.com" };

            Assert.Equal("Ona", MakeQuiz(owner).ToSummary().CreatorName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToSummary_OwnerWithoutDisplayName_UsesEmailPartBeforeAtSign(string? displayName)
        {
            var owner = new ApplicationUser { DisplayName = displayName, Email = "ona.p@example.com" };

            Assert.Equal("ona.p", MakeQuiz(owner).ToSummary().CreatorName);
        }

        [Fact]
        public void ToSummary_OwnerWithoutDisplayNameOrEmail_ReturnsUnknown()
        {
            var owner = new ApplicationUser { DisplayName = null, Email = null };

            Assert.Equal("Unknown", MakeQuiz(owner).ToSummary().CreatorName);
        }

        [Fact]
        public void ToSummary_OwnerNotLoaded_ReturnsUnknown()
        {
            Assert.Equal("Unknown", MakeQuiz(owner: null).ToSummary().CreatorName);
        }



        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToAnswerResponse_AnyOption_CopiesIdTextAndIsCorrect(bool isCorrect)
        {
            var option = new AnswerOption { Id = 3, QuestionId = 1, Text = "Labas", IsCorrect = isCorrect };

            var response = option.ToAnswerResponse();

            Assert.Equal(3, response.Id);
            Assert.Equal("Labas", response.Text);
            Assert.Equal(isCorrect, response.IsCorrect);
        }
    }
}
