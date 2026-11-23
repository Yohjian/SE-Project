using QuattroLingo.Exceptions;

namespace QuattroLingo.Helpers
{
    public static class Limits
    {
        public const int QuizzesPerUser = 50;
        public const int QuestionsPerQuiz = 50;
        public const int AnswerOptionsMax = 6;
        public const int QuizTitleMax = 100;
        public const int QuizDescriptionMax = 500;
        public const int QuestionTextMax = 300;
        public const int AnswerTextMax = 150;
        public const int TimeLimitMinSeconds = 5;
        public const int TimeLimitMaxSeconds = 300;
        public const int PointsMin = 1;
        public const int PointsMax = 1000;

        public const int SetsPerUser = 50;
        public const int CardsPerSet = 500;
        public const int SetNameMax = 100;
        public const int CardTextMax = 150;

        public const long MaxRequestBodyBytes = 1_000_000;

        public static void EnsureMaxLength(string? value, int max, string field)
        {
            if (value is not null && value.Length > max)
                throw new ValidationException($"{field} must be at most {max} characters.");
        }
    }
}