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
    client.BaseAddress = new Uri("http://emissions.api:8080/");
}).AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(3)));

builder.Services.AddHttpClient("MeasurementsApi", client =>
{
    client.BaseAddress = new Uri("http://measurements.api:8080/");   
})
.AddTransientHttpErrorPolicy(policy =>
    policy.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(3))) // Retry policy
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(20)); // Timeout policy

builder.Services.AddScoped<IEmissionCalculatorService, EmissionCalculatorService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

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