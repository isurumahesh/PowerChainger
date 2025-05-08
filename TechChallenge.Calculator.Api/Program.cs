using Polly;
using Serilog;
using TechChallenge.Calculator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddHttpClient("EmissionsApi", client =>
//{
//    client.BaseAddress = new Uri("http://emissions.api:8080/");
//    client.DefaultRequestHeaders.Add("Accept", "application/json");
//}).AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(3)));

//builder.Services.AddHttpClient("MeasurementsApi", client =>
//{
//    client.BaseAddress = new Uri("http://measurements.api:8080/");
//    client.DefaultRequestHeaders.Add("Accept", "application/json");
//})

builder.Services.AddHttpClient("EmissionsApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:44385/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(retryCount: 5,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(3),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Log.Warning(
                "Retry {RetryAttempt} after {Delay}s due to {Exception} or {Result}.",
                retryAttempt,
                timespan.TotalSeconds,
                outcome.Exception?.Message ?? "no exception",
                outcome.Result?.StatusCode
            );
        }));

builder.Services.AddHttpClient("MeasurementsApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:44309/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddTransientHttpErrorPolicy(policy =>
    policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(3))) // Retry policy
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(20)); // Timeout policy

builder.Services.AddScoped<IEmissionCalculatorService, EmissionCalculatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();