namespace TechChallenge.Calculator.Api.Helpers
{
    public static class MeasurementsTimestampHelper
    {
        public static List<long> GetMeasurementsTimeslots(long from, long to, int seed)
        {
            var timeslots = new List<long>();

            while (from < to)
            {
                timeslots.Add(from);
                from = from + seed;

                if (from >= to)
                {
                    timeslots.Add(to);
                }
            }

            return timeslots;
        }
    }
}