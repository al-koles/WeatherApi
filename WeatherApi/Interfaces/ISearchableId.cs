namespace WeatherApi.Interfaces
{
    public interface ISearchableId
    {
        Task<int> GetId(string name);
    }
}
