using Microsoft.EntityFrameworkCore;
using WeatherApi.Data;
using WeatherApi.Interfaces;

namespace WeatherApi.Services
{
    public class CityIdSearcher : ISearchableId
    {
        private readonly WeatherdbContext _context;
        public CityIdSearcher()
        {
            _context = new WeatherdbContext();
        }
        public async Task<int> GetId(string name)
        {
            var cities = await(from c in _context.Cities
                               where c.CityName == name
                               select c.CityId).ToListAsync();
            if (!cities.Any())
            {
                return -1;
            }
            return cities[0];
        }
    }
}
