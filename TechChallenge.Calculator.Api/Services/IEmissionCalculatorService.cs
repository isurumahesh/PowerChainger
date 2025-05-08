using TechChallenge.Calculator.Api.Models;

namespace TechChallenge.Calculator.Api.Services
{
    public interface IEmissionCalculatorService
    {
        Task<List<FinalResponse>> CalculateEmissions(int from, int to, List<string> userIds);

        Task<string> TestEmissionApi();

        Task<string> TestMeasurementApi();
    }
}