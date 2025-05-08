using TechChallenge.Calculator.Api.Models;

namespace TechChallenge.Calculator.Api.Services
{
    public interface IEmissionCalculatorService
    {
        Task<List<UserEmissionData>> CalculateEmissions(int from, int to, List<string> userIds, CancellationToken cancellationToken);
        Task<double> CalculateEmissions2(int from, int to, List<string> userIds);

        Task<string> TestEmissionApi();

        Task<string> TestMeasurementApi();
    }
}