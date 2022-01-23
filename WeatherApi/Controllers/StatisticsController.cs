#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private ISearchableId cityIdFinder;

        public StatisticsController()
        {
            _context = new WeatherdbContext();
            cityIdFinder = new CityIdSearcher();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Statistic>>> GetStatistics()
        {
            return await _context.Statistics.ToListAsync();
        }

        [HttpGet("{city}")]
        public async Task<ActionResult<IEnumerable<Statistic>>> GetStatistics(string city)
        {
            int cityId = await cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            return await _context.Statistics.Where(s=>s.CityId == cityId).OrderBy(s=>s.StatisticsId).ToListAsync();
        }

        [HttpGet("{city}, {fromTime}, {toTime}")]
        public async Task<ActionResult<IEnumerable<Statistic>>> GetStatistics(string city, DateTime fromTime, DateTime toTime)
        {
            int cityId = await cityIdFinder.GetId(city);
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
            return await _context.Statistics.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Statistic>> GetStatistic(int id)
        {
            var statistic = await _context.Statistics.FindAsync(id);

            if (statistic == null)
            {
                return NotFound();
            }

            return statistic;
        }

        [HttpPost("{city}")]
        public async Task<ActionResult<Statistic>> PostStatistic(string city)
        {
            int cityId = await cityIdFinder.GetId(city);
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
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStatistic", new { id = stats.StatisticsId }, stats);
        }

        [HttpPost("{city}, {fromTime}, {toTime}")]
        public async Task<ActionResult<Statistic>> PostStatistic(string city, DateTime fromTime, DateTime toTime)
        {

            int cityId = await cityIdFinder.GetId(city);
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
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStatistic", new { id = stats.StatisticsId }, stats);
        }

        // DELETE: api/Statistics/5
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
