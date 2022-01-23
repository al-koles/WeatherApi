#nullable disable
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
    public class MeasurementsController : ControllerBase
    {
        private readonly WeatherdbContext _context;
        private ISearchableId cityIdFinder;

        public MeasurementsController()
        {
            _context = new WeatherdbContext();
            cityIdFinder = new CityIdSearcher();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Measurement>>> GetMeasurements()
        {
            return await (from m in _context.Measurements orderby m.City, m.Timestamp select m).ToListAsync();
        }

        [HttpGet("{city}, {timestamp}")]
        public async Task<ActionResult<Measurement>> GetMeasurement(string city, DateTime timestamp)
        {
            int cityId = await cityIdFinder.GetId(city);
            if (cityId ==-1)
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
            return measurement[0];
        }

        /// <summary>
        /// Retrieve current weather conditions for a selected city
        /// </summary>
        /// <param name="city"></param>
        /// <returns>Measurement</returns>
        [HttpGet("{city}")]
        public async Task<ActionResult<Measurement>> GetLastMeasurement(string city)
        {
            int cityId = await cityIdFinder.GetId(city);
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
            int cityId = await cityIdFinder.GetId(city);
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
            int cityId = await cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            measurement.CityId = cityId;
            measurement.Timestamp = timestamp;

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
        /// <param name="timestamp1"></param>
        /// <param name="timestamp2"></param>
        /// <returns>No content</returns>
        [HttpPut("{city}, {timestamp1}, {timestamp2}")]
        public async Task<IActionResult> Archieve(string city, DateTime timestamp1, DateTime timestamp2)
        {
            int cityId = await cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            var measurements = await (from m in _context.Measurements
                                      where m.CityId == cityId &&
                                      m.Timestamp >= timestamp1 &&
                                      m.Timestamp <= timestamp2
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
            int cityId = await cityIdFinder.GetId(city);
            if (cityId == -1)
            {
                return NotFound();
            }
            measurement.CityId = cityId;
            measurement.Timestamp = timestamp;
            _context.Measurements.Add(measurement);
            try
            {
                await _context.SaveChangesAsync();
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
            int cityId = await cityIdFinder.GetId(city);
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
