#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WeatherApi.Data;
using WeatherApi.Interfaces;
using WeatherApi.Models;
using WeatherApi.Services;

namespace WeatherApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly WeatherdbContext _context;
        private ISearchableId _cityIdFinder;
        private IMemoryCache _cache;

        public StatisticsController(
            IMemoryCache cache,
            ISearchableId cityIdFinder,
            WeatherdbContext context
            )
        {
            _context = context;
            _cityIdFinder = cityIdFinder;
            _cache = cache;
        }

        /// <summary>
        /// Get all statistics from the db
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Statistic>>> GetStatistics()
        {
            return await _context.Statistics.ToListAsync();
        }

        /// <summary>
        /// Get statistics of a city
        /// </summary>
        /// <param name="city"></param>
        /// <returns>Collection of statistics</returns>
        [HttpGet("{city}")]
        public async Task<ActionResult<IEnumerable<Statistic>>> GetStatistics(string city)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            return await _context.Statistics.Where(s=>s.CityId == cityId).OrderBy(s=>s.StatisticsId).ToListAsync();
        }

        /// <summary>
        /// Get statistics of a city of the time period
        /// </summary>
        /// <param name="city"></param>
        /// <param name="fromTime"></param>
        /// <param name="toTime"></param>
        /// <returns>Collection of statistics</returns>
        [HttpGet("{city}, {fromTime}, {toTime}")]
        public async Task<ActionResult<IEnumerable<Statistic>>> GetStatistics(string city, DateTime fromTime, DateTime toTime)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var statistics = await (from s in _context.Statistics
                                      where s.CityId == cityId &&
                                      s.FromTime >= fromTime &&
                                      s.ToTime <= toTime
                                      select s).ToListAsync();
            if (!statistics.Any())
            {
                return NotFound();
            }
            return statistics;
        }

        /// <summary>
        /// Get statistics by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Statistics</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Statistic>> GetStatistic(int id)
        {
            Statistic statistic;
            if(!_cache.TryGetValue(id, out statistic))
            {
                statistic = await _context.Statistics.FindAsync(id);

                if (statistic == null)
                {
                    return NotFound();
                }
            }
            
            return statistic;
        }

        /// <summary>
        /// Post statistics for the city for all time
        /// </summary>
        /// <param name="city"></param>
        /// <returns>Statistics</returns>
        [HttpPost("{city}")]
        public async Task<ActionResult<Statistic>> PostStatistic(string city)
        {
            int cityId = await _cityIdFinder.GetId(city);
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
            var firstMeasurementTime = await _context.Measurements.Where(s => s.CityId == cityId).MinAsync(s => s.Timestamp);
            var lastMeasurement = await Task.Run(()=>measurements.MaxBy(x => x.Timestamp));
            Statistic stats = new Statistic();
            stats.CityId = cityId;
            stats.LastMeasurementTemperature = lastMeasurement!.Temperature;
            stats.LastMeasurementTime = lastMeasurement.Timestamp;
            stats.AvgTemperature = measurements.Average(m => m.Temperature);
            stats.MaxTemperature = measurements.Max(m => m.Temperature);
            stats.MinTemperature = measurements.Min(m => m.Temperature);
            stats.FromTime = firstMeasurementTime;
            stats.ToTime = lastMeasurement.Timestamp;


            _context.Statistics.Add(stats);
            var n = await _context.SaveChangesAsync();
            if (n > 0)
            {
                _cache.Set(
                    stats.StatisticsId,
                    stats,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    }
                    );
            }

            return CreatedAtAction("GetStatistic", new { id = stats.StatisticsId }, stats);
        }

        /// <summary>
        /// Post statistics for the city for the time period
        /// </summary>
        /// <param name="city"></param>
        /// <param name="fromTime"></param>
        /// <param name="toTime"></param>
        /// <returns></returns>
        [HttpPost("{city}, {fromTime}, {toTime}")]
        public async Task<ActionResult<Statistic>> PostStatistic(string city, DateTime fromTime, DateTime toTime)
        {

            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurements = await (from m in _context.Measurements
                                      where m.CityId == cityId &&
                                      m.IsArchived == false &&
                                      m.Timestamp >= fromTime &&
                                      m.Timestamp <= toTime
                                      select m).ToListAsync();
            if (!measurements.Any())
            {
                return NotFound();
            }
            var lastMeasurement = measurements.MaxBy(x => x.Timestamp);
            Statistic stats = new Statistic();
            stats.CityId = cityId;
            stats.LastMeasurementTemperature = lastMeasurement!.Temperature;
            stats.LastMeasurementTime = lastMeasurement.Timestamp;
            stats.AvgTemperature = measurements.Average(m => m.Temperature);
            stats.MaxTemperature = measurements.Max(m => m.Temperature);
            stats.MinTemperature = measurements.Min(m => m.Temperature);
            stats.FromTime = fromTime;
            stats.ToTime = toTime;


            _context.Statistics.Add(stats);
            var n = await _context.SaveChangesAsync();
            if (n > 0)
            {
                _cache.Set(
                    stats.StatisticsId,
                    stats,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    }
                    );
            }

            return CreatedAtAction("GetStatistic", new { id = stats.StatisticsId }, stats);
        }

        /// <summary>
        /// Delete statistics by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatistic(int id)
        {
            var statistic = await _context.Statistics.FindAsync(id);
            if (statistic == null)
            {
                return NotFound();
            }

            _context.Statistics.Remove(statistic);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StatisticExists(int id)
        {
            return _context.Statistics.Any(e => e.StatisticsId == id);
        }

    }
}
