namespace QuattroLingo.Models
{
    public readonly record struct Score(int Points) : IComparable<Score>
    {
        public static Score FromAnswerTime(
            TimeSpan elapsed,
            int timeLimitSeconds,
            int maxPoints = 100,
            double minFraction = 0.5)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeLimitSeconds);

            var ratio = Math.Clamp(elapsed.TotalSeconds / timeLimitSeconds, 0, 1);
            var fraction = 1 - ratio * (1 - minFraction);

            return new Score((int)Math.Round(maxPoints * fraction, MidpointRounding.AwayFromZero));
        }

        public int CompareTo(Score other) => Points.CompareTo(other.Points);
    }
}
