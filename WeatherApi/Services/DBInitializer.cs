using WeatherApi.Data;
using WeatherApi.Interfaces;
using WeatherApi.Models;

namespace WeatherApi.Services
{
    public static class DBInitializer
    {
        public static void Initialize(WeatherdbContext context)
        {
            if(context.Cities.Any() || context.Measurements.Any())
            {
                return;
            }

            var cities = new City[]
            {
                new City{CityName = "Kharkiv"},
                new City{CityName = "Dnipro"},
                new City{CityName = "Poltava"},
            };
            foreach (var city in cities)
            {
                context.Cities.Add(city);
            }
            context.SaveChanges();

            ISearchableId idSearcher = new CityIdSearcher();
            int id1 = idSearcher.GetId("Kharkiv").Result;
            int id2 = idSearcher.GetId("Dnipro").Result;
            if(id1 == -1 || id2 == -1)
            {
                return;
            }
            var measurements = new Measurement[]
            {
                new Measurement{CityId = id1, Temperature = 10, Timestamp = DateTime.UtcNow},
                new Measurement{CityId = id1, Temperature = 1, Timestamp = DateTime.UtcNow-TimeSpan.FromHours(1)},
                new Measurement{CityId = id1, Temperature = 12, Timestamp = DateTime.UtcNow-TimeSpan.FromHours(2)},
                new Measurement{CityId = id2, Temperature = 4, Timestamp = DateTime.UtcNow-TimeSpan.FromHours(15)},
                new Measurement{CityId = id2, Temperature = 6, Timestamp = DateTime.UtcNow-TimeSpan.FromHours(8)},
                new Measurement{CityId = id2, Temperature = -1, Timestamp = DateTime.UtcNow-TimeSpan.FromHours(12)}
            };
            foreach(var measurement in measurements)
            {
                context.Measurements.Add(measurement);
            }
            context.SaveChanges();

        }
    }
}
