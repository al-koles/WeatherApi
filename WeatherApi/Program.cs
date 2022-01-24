
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WeatherApi;
using WeatherApi.Data;
using WeatherApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCityIdSearcher();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<WeatherdbContext>();

var app = builder.Build();

//Configure the HTTP request pipeline.

try
{
    WeatherdbContext weatherContext = new WeatherdbContext();
    if (weatherContext.Database.EnsureCreated())
    {
        DBInitializer.Initialize(weatherContext);
    }
}
catch (Exception ex)
{
    app.Run(async context => await context.Response.WriteAsync($"Error ocured connecting to the database... " +
    $"Please check out connection string in file appsettings.json and SQL Server is configured.\n\nError message: {ex.Message}", 
    System.Text.Encoding.Default));
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
