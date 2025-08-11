using Microsoft.AspNetCore.Cors.Infrastructure;
using NewsArticles.Models.Configurations;
using NewsArticles.Services.Implementations;
using NewsArticles.Services.Interfaces;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string CorsPolicy = "AllowAngular4200";

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200", "https://polite-forest-0a8180a10.2.azurestaticapps.net")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddHttpClient<INewsStoriesClientService, NewsStoriesClientService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["NewsStoriesCacheOptions:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * (retry + 1))));

builder.Services.Configure<NewsStoriesCacheOptions>(builder.Configuration.GetSection("NewsStoriesCacheOptions"));

builder.Services.AddScoped<INewsStoriesService, NewsStoriesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors(CorsPolicy);
app.MapControllers();

app.Run();
