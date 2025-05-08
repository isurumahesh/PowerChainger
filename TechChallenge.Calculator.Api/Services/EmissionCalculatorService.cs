using System.Collections.Concurrent;
using TechChallenge.Calculator.Api.Helpers;
using TechChallenge.Calculator.Api.Models;

namespace TechChallenge.Calculator.Api.Services
{
    public class EmissionCalculatorService(IHttpClientFactory httpClientFactory, ILogger<EmissionCalculatorService> logger) : IEmissionCalculatorService
    {
        public async Task<List<FinalResponse>> CalculateEmissions(int from, int to, List<string> userIds)
        {
            try
            {
                var finalResponseList = new List<FinalResponse>();
                var tasks = new List<Task<List<MeasurementResponse>>>();
                var emissionClient = httpClientFactory.CreateClient("EmissionsApi");
                var emissionResponse = await emissionClient.GetFromJsonAsync<List<EmissionResponse>>($"emissions?from={from}&to={to}");

                if (emissionResponse == null || !emissionResponse.Any() || emissionResponse.Where(a => a.KgPerWattHr == 0).Any())
                {
                    throw new Exception("Retrieving Emission error");
                }

                var measurementsTimeSlots = MeasurementsTimestampHelper.GetMeasurementsTimeslots(from, to, 7200);
                var measurementClient = httpClientFactory.CreateClient("MeasurementsApi");

                foreach (var item in userIds)
                {
                    try
                    {
                        for (int i = 0; i < measurementsTimeSlots.Count - 1; i++)
                        {
                            int currentIndex = i;
                            tasks.Add(Task.Run(() => FetchDataAsync(measurementClient, measurementsTimeSlots[currentIndex], measurementsTimeSlots[currentIndex + 1], item)));
                        }

                        var responses = await Task.WhenAll(tasks);

                        if (responses is null || !responses.Any())
                        {
                            throw new Exception("Retrieving measurements error");
                        }

                        var concurrentMeasurements = new ConcurrentBag<MeasurementResponse>();

                        Parallel.ForEach(responses, m =>
                        {
                            foreach (var item in m)
                            {
                                concurrentMeasurements.Add(item);
                            }
                        });

                        long fromTimeStamp = from;
                        var calculations = new ConcurrentBag<double>();
                        object lockObj = new object();

                        var parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount - 1
                        };

                        Parallel.ForEach(emissionResponse, parallelOptions, a =>
                        {
                            long localFromTimeStamp;

                            lock (lockObj)
                            {
                                localFromTimeStamp = fromTimeStamp;
                                fromTimeStamp = a.Timestamp;
                            }

                            var measurements = concurrentMeasurements.Where(m => m.Timestamp >= localFromTimeStamp && m.Timestamp <= a.Timestamp).ToList();
                            double allWats = concurrentMeasurements.Sum(a => a.Watts);
                            double denominator = concurrentMeasurements.Count * 4 * 1000;
                            double allWatsInKwh = denominator != 0 ? allWats / denominator : 0;

                            if (allWatsInKwh == 0)
                            {
                                logger.LogInformation($"allWatsInKwh:{allWatsInKwh} allWats:{allWats} denominator :{denominator}");
                            }

                            var result = a.KgPerWattHr * allWatsInKwh;
                            calculations.Add(result);
                        });

                        double finalResult = calculations.Sum(a => a);

                        finalResponseList.Add(new FinalResponse(item, finalResult));
                    }
                    catch (Exception ex)
                    {
                        finalResponseList.Add(new FinalResponse(item, 0));
                        logger.LogError(ex, $"Calculation error for user:{item}");
                    }
                }

                return finalResponseList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Calculation error");
                throw;
            }
        }

        private async Task<List<MeasurementResponse>> FetchDataAsync(HttpClient measurementClient, long from, long to, string userId)
        {
            try
            {
                return await measurementClient.GetFromJsonAsync<List<MeasurementResponse>>($"measurements/{userId}?from={from}&to={to}");

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Measurementapi error: measurements/{userId}?from={from}&to={to}");
                throw;
            }
        }

        public async Task<string> TestEmissionApi()
        {
            var emissionClient = httpClientFactory.CreateClient("EmissionsApi");
            var emissionResponse = await emissionClient.GetFromJsonAsync<string>($"emissionstest");
            return emissionResponse;
        }

        public async Task<string> TestMeasurementApi()
        {
            var emissionClient = httpClientFactory.CreateClient("MeasurementsApi");
            var emissionResponse = await emissionClient.GetFromJsonAsync<string>($"measurementstest");
            return emissionResponse;
        }
    }
}