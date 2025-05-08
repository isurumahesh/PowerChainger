using Microsoft.AspNetCore.Mvc;
using TechChallenge.Calculator.Api.Services;

namespace TechChallenge.Calculator.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculatorController(IEmissionCalculatorService emissionCalculatorService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetTotalEmission([FromQuery] int from, [FromQuery] int to, [FromQuery] List<string> userIds, CancellationToken cancellationToken)
        {
            if (from == 0 || to == 0 || !userIds.Any())
            {
                return BadRequest();
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (from > currentTime || to > currentTime || from >= to)
            {
                return BadRequest();
            }

            var response = await emissionCalculatorService.CalculateEmissions(from, to, userIds, cancellationToken);
            return Ok(response);
        }

        [HttpGet("test2")]
        public async Task<IActionResult> GetTotalEmission2([FromQuery] int from, [FromQuery] int to, [FromQuery] List<string> userIds)
        {
            if (from == 0 || to == 0 || !userIds.Any())
            {
                return BadRequest();
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (from > currentTime || to > currentTime || from >= to)
            {
                return BadRequest();
            }

            var response = await emissionCalculatorService.CalculateEmissions2(from, to, userIds);
            return Ok(response);
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            return Ok("test");
        }

        [HttpGet("testemission")]
        public async Task<IActionResult> TestEmission()
        {
            var response = await emissionCalculatorService.TestEmissionApi();
            return Ok(response);
        }

        [HttpGet("testmeasurement")]
        public async Task<IActionResult> TestMeasurement()
        {
            var response = await emissionCalculatorService.TestMeasurementApi();
            return Ok(response);
        }
    }
}