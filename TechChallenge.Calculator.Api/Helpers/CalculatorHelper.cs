namespace TechChallenge.Calculator.Api.Helpers
{
    public static class CalculatorHelper
    {
        public static int CacheDurationInMinutes = 5;
        public static int MeasurementsIntervalInSeconds = 7200;
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

        public static string GetCacheKey(long from, long to, string userId)
        {

            return $"CacheKey-{from}-{to}-{userId}";
        }
    }
}