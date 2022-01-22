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
        public class TemperatureStatistics
        {
            public string? СityName { get; set; }
            public int Temperature { get; set; }
            public DateTime LastMeasurementTimestamp { get; set; }
            public double AvgTemp { get; set; }
            public int MaxTemp { get; set; }
            public int MinTemp { get; set; }
        }

        /// <summary>
        /// Average, max and min temperature for the whole time of measurements
        /// </summary>
        /// <returns>Temperature statistics</returns>
        [HttpGet("{city}")]
        public async Task<ActionResult<TemperatureStatistics>> GetTemperatureStatistics(string city)
        {
            int cityId = await GetCityId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurements = await (from m in _context.Measurements
                                      where m.CityId == cityId &&
                                      m.IsArchived == false
                                      select m).ToListAsync();
            if (!measurements.Any())
            {
                return NotFound();
            }
            var lastMeasurement = measurements.MaxBy(x => x.Timestamp);
            TemperatureStatistics stats = new TemperatureStatistics();
            stats.СityName = city;
            stats.Temperature = lastMeasurement!.Temperature;
            stats.LastMeasurementTimestamp = lastMeasurement.Timestamp;
            stats.AvgTemp = measurements.Average(m=>m.Temperature);
            stats.MaxTemp = measurements.Max(m=>m.Temperature);
            stats.MinTemp = measurements.Min(m=>m.Temperature);

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
