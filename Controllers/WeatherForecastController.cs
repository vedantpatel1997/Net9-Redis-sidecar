using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace RedisNet9.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IDistributedCache _cache;

        private const string CacheKey = "weather_forecast";

        public WeatherForecastController(IDistributedCache cache, ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            _cache = cache;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            // 1. Try get from cache
            var cached = await _cache.GetStringAsync(CacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("✅ Returning WeatherForecast from Redis cache");
                return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(cached)!;
            }

            // 2. Otherwise generate new forecasts
            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            // 3. Store in cache with expiration
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) // cache for 1 minute
            };

            var serialized = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(CacheKey, serialized, options);

            _logger.LogInformation("🆕 Cached new WeatherForecast data in Redis");

            return result;
        }
    }
}   
