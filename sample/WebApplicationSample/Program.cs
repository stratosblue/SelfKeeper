using SelfKeeper;

var logger = new DefaultConsoleLogger("WebApplicationSample");

logger.Info($"Hello, World! 【{Environment.OSVersion}】");

KeepSelf.Handle(args, options => options.RemoveFlag(KeepSelfFeatureFlag.SkipWhenDebuggerAttached | KeepSelfFeatureFlag.DisableForceKillByHost));

logger.Info($"SelfKeeperEnvironment IsWorkerProcess: {SelfKeeperEnvironment.IsWorkerProcess}, SessionId: {SelfKeeperEnvironment.SessionId}");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
       new WeatherForecast
       (
           DateTime.Now.AddDays(index),
           Random.Shared.Next(-20, 55),
           summaries[Random.Shared.Next(summaries.Length)]
       ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
