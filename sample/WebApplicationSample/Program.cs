using SelfKeeper;

Log($"Hello, World! ¡¾{Environment.OSVersion}¡¿");

KeepSelf.Handle(args, options => options.RemoveFlag(KeepSelfFeatureFlag.SkipWhenDebuggerAttached | KeepSelfFeatureFlag.DisableForceKillByHost));

Log($"SelfKeeperEnvironment IsChildProcess: {SelfKeeperEnvironment.IsChildProcess}, SessionId: {SelfKeeperEnvironment.SessionId}");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

static void Log(string message, ConsoleColor? color = null)
{
    if (color.HasValue)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{Environment.ProcessId}] {message}");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine($"[{Environment.ProcessId}] {message}");
    }
}

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
