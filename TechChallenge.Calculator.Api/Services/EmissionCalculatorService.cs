using System.Collections.Concurrent;
using System.Threading;
using TechChallenge.Calculator.Api.Helpers;
using TechChallenge.Calculator.Api.Models;

namespace TechChallenge.Calculator.Api.Services
{
    public class EmissionCalculatorService(IHttpClientFactory httpClientFactory, ILogger<EmissionCalculatorService> logger, ICacheService cacheService) : IEmissionCalculatorService
    {
        public async Task<List<UserEmissionData>> CalculateEmissions(int from, int to, List<string> userIds, CancellationToken cancellationToken)
        {
            try
            {

                var useEmissionsDataLIst = new List<UserEmissionData>();
                var tasks = new List<Task<List<MeasurementResponse>>>();
                var emissionClient = httpClientFactory.CreateClient("EmissionsApi");
                var emissionResponse = await emissionClient.GetFromJsonAsync<List<EmissionResponse>>($"emissions?from={from}&to={to}", cancellationToken);

                if (emissionResponse == null || !emissionResponse.Any() || emissionResponse.Where(a => a.KgPerWattHr == 0).Any())
                {
                    throw new Exception("Retrieving Emission error");
                }

                var measurementsTimeSlots = CalculatorHelper.GetMeasurementsTimeslots(from, to, CalculatorHelper.MeasurementsIntervalInSeconds);
                var measurementClient = httpClientFactory.CreateClient("MeasurementsApi");

                foreach (var userId in userIds)
                {
                    try
                    {
                        var cacheKey = CalculatorHelper.GetCacheKey(from, to, userId);
                        var cachedValue = cacheService.Get<double>(cacheKey);

                        if (cachedValue != 0)
                        {
                            useEmissionsDataLIst.Add(new UserEmissionData(userId, cachedValue));
                            continue;
                        }


                        for (int i = 0; i < measurementsTimeSlots.Count - 1; i++)
                        {
                            int currentIndex = i;
                            tasks.Add(Task.Run(() => FetchDataAsync(measurementClient, measurementsTimeSlots[currentIndex], measurementsTimeSlots[currentIndex + 1], userId, cancellationToken)));
                        }

                        var responses = await Task.WhenAll(tasks);

                        if (responses is null || !responses.Any())
                        {
                            throw new Exception("Retrieving measurements error");
                        }

                        var parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount - 1
                        };

                        var concurrentMeasurements = new ConcurrentBag<MeasurementResponse>();

                        Parallel.ForEach(responses, parallelOptions, measurememts =>
                        {
                            foreach (var item in measurememts)
                            {
                                concurrentMeasurements.Add(item);
                            }
                        });

                        long fromTimeStamp = from;
                        var calculations = new ConcurrentBag<double>();
                        var timeInterval = 900;
                
                        Parallel.ForEach(emissionResponse, parallelOptions, a =>
                        {                           
                            var measurements = concurrentMeasurements.Where(m => m.Timestamp >= a.Timestamp - timeInterval && m.Timestamp <= a.Timestamp).ToList();
                            double allWatts = measurements.Sum(a => a.Watts);
                            double denominator = measurements.Count * 4 * 1000;
                            double allWattsInKwh = denominator != 0 ? allWatts / denominator : 0;

                            if (allWattsInKwh == 0)
                            {
                                logger.LogInformation($"allWatsInKwh:{allWattsInKwh} allWats:{allWatts} denominator :{denominator}");
                            }

                            var result = a.KgPerWattHr * allWattsInKwh;
                            calculations.Add(result);
                        });


                        //foreach (var item in emissionResponse)
                        //{

                        //    long localFromTimeStamp = fromTimeStamp;
                        //    fromTimeStamp = item.Timestamp;
                        //    var measurements = concurrentMeasurements.Where(m => m.Timestamp >= localFromTimeStamp && m.Timestamp <= item.Timestamp).ToList();
                        //    double allWatts = measurements.Sum(a => a.Watts);
                        //    double denominator = measurements.Count * 4 * 1000;
                        //    double allWattsInKwh = denominator != 0 ? allWatts / denominator : 0;

                        //    if (allWattsInKwh == 0)
                        //    {
                        //        logger.LogInformation($"allWatsInKwh:{allWattsInKwh} allWats:{allWatts} denominator :{denominator}");
                        //    }

                        //    var result = item.KgPerWattHr * allWattsInKwh;
                        //    calculations.Add(result);
                        //}
                        

                        double finalResult = calculations.Sum(a => a);
                        useEmissionsDataLIst.Add(new UserEmissionData(userId, finalResult));
                        cacheService.Set(cacheKey, finalResult, TimeSpan.FromMinutes(CalculatorHelper.CacheDurationInMinutes));
                    }
                    catch (Exception ex)
                    {
                        useEmissionsDataLIst.Add(new UserEmissionData(userId, 0));
                        logger.LogError(ex, $"Calculation error for user:{userId}");
                    }
                }

                return useEmissionsDataLIst;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Calculation error");
                throw;
            }
        }

        private async Task<List<MeasurementResponse>> FetchDataAsync(HttpClient measurementClient, long from, long to, string userId, CancellationToken cancellationToken)
        {
            try
            {
                return await measurementClient.GetFromJsonAsync<List<MeasurementResponse>>($"measurements/{userId}?from={from}&to={to}", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Measurementapi error: measurements/{userId}?from={from}&to={to}");
                throw;
            }
        }

    }
}