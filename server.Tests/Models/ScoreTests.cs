using QuattroLingo.Models;

namespace QuattroLingo.Tests.Models
{
    public class ScoreTests
    {
        [Theory]
        [InlineData(0, 100)]      // instant answer
        [InlineData(3, 100)]      // inside grace period
        [InlineData(6, 100)]      // exactly at grace limit
        [InlineData(15.9, 100)]   // just before the first decay step
        [InlineData(16, 95)]      // first decay step
        [InlineData(26, 90)]      // second decay step
        public void FromAnswerTime_UsesDefaultRules(double seconds, int expectedPoints)
        {
            var score = Score.FromAnswerTime(TimeSpan.FromSeconds(seconds));

            Assert.Equal(expectedPoints, score.Points);
        }

        [Fact]
        public void FromAnswerTime_NeverGoesBelowZero()
        {
            var score = Score.FromAnswerTime(TimeSpan.FromSeconds(1000));

            Assert.Equal(0, score.Points);
        }

        [Fact]
        public void FromAnswerTime_AcceptsNamedArguments()
        {
            // overtime = 10s, steps = 10 / 5 = 2, points = 200 - 2 * 10 = 180
            var score = Score.FromAnswerTime(
                TimeSpan.FromSeconds(15),
                maxPoints: 200,
                decayPerStep: 10,
                graceSeconds: 5,
                stepSeconds: 5);

            Assert.Equal(180, score.Points);
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
            var scores = new List<Score> { new(30), new(90), new(60) };

            scores.Sort();

            Assert.Equal(new[] { 30, 60, 90 }, scores.Select(s => s.Points));
        }

        [Fact]
        public void Scores_WithSamePoints_AreEqual()
        {
            Assert.Equal(new Score(75), new Score(75));
        }
    }
}