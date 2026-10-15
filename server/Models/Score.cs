namespace QuattroLingo.Models
{
    public readonly record struct Score(int Points) : IComparable<Score>
    {
        public static Score FromAnswerTime(
            TimeSpan elapsed,
            int maxPoints = 100,
            int decayPerStep = 5,
            int graceSeconds = 6,
            int stepSeconds = 10)
        {
            var overtime = elapsed.TotalSeconds - graceSeconds;
            var steps = overtime <= 0 ? 0 : (int)(overtime / stepSeconds);
            return new Score(Math.Max(0, maxPoints - steps * decayPerStep));
        }

        public int CompareTo(Score other) => Points.CompareTo(other.Points);
    }
}