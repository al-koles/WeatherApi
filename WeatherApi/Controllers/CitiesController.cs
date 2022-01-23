#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApi.Data;
using WeatherApi.Models;

namespace WeatherApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly WeatherdbContext _context;

        public CitiesController()
        {
            _context = new WeatherdbContext();
        }

        /// <summary>
        /// Get all cities
        /// </summary>
        /// <returns>Collection of cities</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<City>>> GetCities()
        {
            return await _context.Cities.ToListAsync();
        }

        /// <summary>
        /// Get city by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>City</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<City>> GetCity(int id)
        {
            var city = await _context.Cities.FindAsync(id);

            if (city == null)
            {
                return NotFound();
            }

            return city;
        }

        /// <summary>
        /// Edit city name
        /// </summary>
        /// <param name="id"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCity(int id, City city)
        {
            city.CityId = id;
            _context.Entry(city).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CityExists(id))
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
        /// Post new city
        /// </summary>
        /// <param name="city"></param>
        /// <returns>City</returns>
        [HttpPost]
        public async Task<ActionResult<City>> PostCity(City city)
        {
            _context.Cities.Add(city);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return BadRequest();
            }
            return CreatedAtAction("GetCity", new { id = city.CityId }, city);
        }

        /// <summary>
        /// Delete city by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null)
            {
                return NotFound();
            }

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CityExists(int id)
        {
            return _context.Cities.Any(e => e.CityId == id);
        }
    }
}
