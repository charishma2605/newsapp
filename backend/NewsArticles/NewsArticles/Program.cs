using NewsArticles.Models.Configurations;
using NewsArticles.Services.Implementations;
using NewsArticles.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.Configure<NewsStoriesCacheOptions>(builder.Configuration.GetSection("HackerNews"));

builder.Services.AddScoped<INewsStoriesClientService,NewsStoriesClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
