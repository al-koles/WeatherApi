#nullable disable
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
    public class MeasurementsController : ControllerBase
    {
        private readonly WeatherdbContext _context;
        private ISearchableId _cityIdFinder;
        private IMemoryCache _cache;

        public MeasurementsController(
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
        /// Get all measurements from the database
        /// </summary>
        /// <returns>Collection of measurements</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Measurement>>> GetMeasurements()
        {
            return await (from m in _context.Measurements orderby m.City, m.Timestamp select m).ToListAsync();
        }

        /// <summary>
        /// Get a measurement
        /// </summary>
        /// <param name="city"></param>
        /// <param name="timestamp"></param>
        /// <returns>Measurement</returns>
        [HttpGet("{city}, {timestamp}")]
        public async Task<ActionResult<Measurement>> GetMeasurement(string city, DateTime timestamp)
        {
            Measurement me = null;
            if(!_cache.TryGetValue($"{city}@{timestamp}", out me))
            {
                int cityId = await _cityIdFinder.GetId(city);
                if (cityId == -1)
                {
                    return NotFound();
                }
                var measurement = await (from m in _context.Measurements
                                         where m.CityId == cityId &&
                                         m.Timestamp == timestamp
                                         select m).ToListAsync();
                if (!measurement.Any())
                {
                    return NotFound();
                }
                me = measurement[0];
            }
            return me;
        }

        /// <summary>
        /// Retrieve current weather conditions for a selected city
        /// </summary>
        /// <param name="city"></param>
        /// <returns>Measurement</returns>
        [HttpGet("{city}")]
        public async Task<ActionResult<Measurement>> GetLastMeasurement(string city)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurement = (await (from m in _context.Measurements
                                      where m.CityId == cityId
                                      select m).ToListAsync()).MaxBy(x => x.Timestamp);
            if (measurement == null)
            {
                return NotFound();
            }
            return measurement;
        }

        /// <summary>
        /// Retrieve history of weather conditions for a selected city
        /// </summary>
        /// <param name="city"></param>
        /// <returns>List of measurements</returns>
        [HttpGet("{city}")]
        public async Task<ActionResult<IEnumerable<Measurement>>> GetCityMeasurements(string city)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurements = await (from m in _context.Measurements
                                      where m.CityId == cityId
                                      orderby m.Timestamp
                                      select m).ToListAsync();
            if (!measurements.Any())
            {
                return NotFound();
            }
            return measurements;
        }

        /// <summary>
        /// Update weather condition for certain city at certain point of time
        /// </summary>
        /// <param name="city"></param>
        /// <param name="timestamp"></param>
        /// <param name="measurement"></param>
        /// <returns>No content</returns>
        [HttpPut("{city}, {timestamp}")]
        public async Task<IActionResult> PutMeasurement(string city, DateTime timestamp, Measurement measurement)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            measurement.CityId = cityId;
            measurement.Timestamp = timestamp;
            measurement.City = null;

            _context.Entry(measurement).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MeasurementExists(cityId, timestamp))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Archive weather condition for certain city for certain period of time
        /// - archived data should not be used in statistical calculations
        /// </summary>
        /// <param name="city"></param>
        /// <param name="fromTime"></param>
        /// <param name="toTime"></param>
        /// <returns>No content</returns>
        [HttpPut("{city}, {fromTime}, {toTime}")]
        public async Task<IActionResult> Archive(string city, DateTime fromTime, DateTime toTime)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurements = await (from m in _context.Measurements
                                      where m.CityId == cityId &&
                                      m.Timestamp >= fromTime &&
                                      m.Timestamp <= toTime
                                      select m).ToListAsync();
            if (!measurements.Any())
            {
                return NotFound();
            }
            measurements.ForEach((m) => m.IsArchived = true);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Add weather condition for certain city at certain point of time
        /// </summary>
        /// <param name="measurement"></param>
        /// <returns>Created row</returns>
        [HttpPost("{city}, {timestamp}")]
        public async Task<ActionResult<Measurement>> PostMeasurement(string city, DateTime timestamp, Measurement measurement)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            measurement.CityId = cityId;
            measurement.Timestamp = timestamp;
            measurement.City = null;
            _context.Measurements.Add(measurement);
            try
            {
                var n = await _context.SaveChangesAsync();
                if (n > 0)
                {
                    _cache.Set(
                        $"{city}@{timestamp}", 
                        measurement, 
                        new MemoryCacheEntryOptions 
                        { 
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
                        }
                        );
                }
            }
            catch (DbUpdateException)
            {
                if (MeasurementExists(cityId, timestamp))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetMeasurement", new { city = city, timestamp = measurement.Timestamp }, measurement);
        }

        /// <summary>
        /// Delete wheather condition
        /// </summary>
        /// <param name="city"></param>
        /// <param name="timestamp"></param>
        /// <returns>No content</returns>
        [HttpDelete("{city}, {timestamp}")]
        public async Task<IActionResult> DeleteMeasurement(string city, DateTime timestamp)
        {
            int cityId = await _cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurement = await (from m in _context.Measurements
                                     where m.CityId == cityId &&
                                     m.Timestamp == timestamp
                                     select m).ToListAsync();

            if (!measurement.Any())
            {
                return NotFound();
            }

            _context.Measurements.Remove(measurement[0]);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MeasurementExists(int cityId, DateTime timestamp)
        {
            return _context.Measurements.Any(e => e.CityId == cityId && e.Timestamp == timestamp);
        }
    }
}
