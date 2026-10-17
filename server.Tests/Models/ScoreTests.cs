using QuattroLingo.Models;

namespace QuattroLingo.Tests.Models
{
    public class ScoreTests
    {
        [Theory]
        [InlineData(0, 100)]    // instant
        [InlineData(10, 75)]    // half the time
        [InlineData(20, 50)]    // at the limit
        [InlineData(40, 50)]    // after the limit, clamped
        public void FromAnswerTime_ScalesWithTimeLimit(double seconds, int expectedPoints)
        {
            var score = Score.FromAnswerTime(TimeSpan.FromSeconds(seconds), timeLimitSeconds: 20);

            Assert.Equal(expectedPoints, score.Points);
        }

        [Fact]
        public void FromAnswerTime_ScalesWithMaxPoints()
        {
            var score = Score.FromAnswerTime(TimeSpan.FromSeconds(10), timeLimitSeconds: 20, maxPoints: 1000);

            Assert.Equal(750, score.Points);
        }

        [Fact]
        public void FromAnswerTime_AcceptsNamedArguments()
        {
            var score = Score.FromAnswerTime(
                TimeSpan.FromSeconds(10),
                timeLimitSeconds: 20,
                maxPoints: 200,
                minFraction: 0);

            Assert.Equal(100, score.Points);
        }

        [Fact]
        public void FromAnswerTime_RejectsNonPositiveTimeLimit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Score.FromAnswerTime(TimeSpan.FromSeconds(1), timeLimitSeconds: 0));
        }

        [Fact]
        public void CompareTo_OrdersByPoints()
        {
            var low = new Score(50);
            var high = new Score(100);

            Assert.True(low.CompareTo(high) < 0);
            Assert.True(high.CompareTo(low) > 0);
            Assert.Equal(0, low.CompareTo(new Score(50)));
        }

        [Fact]
        public void Scores_CanBeSortedWithStandardSort()
        {
            List<Score> scores = [new(30), new(90), new(60)];

            scores.Sort();

            int[] expected = [30, 60, 90];
            Assert.Equal(expected, scores.Select(s => s.Points));
        }

        [Fact]
        public void Scores_WithSamePoints_AreEqual()
        {
            Assert.Equal(new Score(75), new Score(75));
        }
    }
}