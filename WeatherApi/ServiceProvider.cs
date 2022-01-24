using WeatherApi.Interfaces;
using WeatherApi.Services;

namespace WeatherApi
{
    public static class ServiceProvider
    {
        public static void AddCityIdSearcher(this IServiceCollection services)
        {
            services.AddTransient<ISearchableId, CityIdSearcher>();
        }
    }
}
