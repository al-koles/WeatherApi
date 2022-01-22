using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApi.Data;

namespace WeatherApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly WeatherdbContext _context;

        public StatisticsController()
        {
            _context = new WeatherdbContext();
        }
        public class Statistics
        {
            public string cityName;
            public int temperature;
            public DateTime lastMeasurementTimestamp;
            public double Avg;
            public int Max;
            public int Min;
        }

        /// <summary>
        /// Average, max and min temperature for the whole time of measurements
        /// </summary>
        /// <returns>Temperature statistics</returns>
        [HttpGet("{city}")]
        public async Task<ActionResult<Statistics>> GetTemperatureStatistics(string city)
        {
            int cityId = await GetCityId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurements = await (from m in _context.Measurements
                                      where m.CityId == cityId
                                      select m).ToListAsync();
            if (!measurements.Any())
            {
                return NotFound();
            }
            var lastMeasurement = measurements.MaxBy(x => x.Timestamp);
            Statistics stats = new Statistics();
            stats.cityName = city;
            stats.temperature = lastMeasurement!.Temperature;
            stats.lastMeasurementTimestamp = lastMeasurement.Timestamp;
            stats.Avg = measurements.Average(m=>m.Temperature);
            stats.Max = measurements.Max(m=>m.Temperature);
            stats.Min = measurements.Min(m=>m.Temperature);

            return stats;
        }

        private async Task<int> GetCityId(string cityName)
        {
            var cities = await (from c in _context.Cities
                                where c.CityName == cityName
                                select c.CityId).ToListAsync();
            if (!cities.Any())
            {
                return -1;
            }
            return cities[0];
        }
    }
}
