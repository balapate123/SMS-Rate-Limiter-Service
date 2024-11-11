using TestRateLimiterService.Services.RateLimiterService;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add CORS configuration to allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddMemoryCache(); // Add memory cache service

// Register RateLimiterService with IConfiguration
builder.Services.AddSingleton<RateLimiterService>(provider =>
{
    var redis = provider.GetRequiredService<IConnectionMultiplexer>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new RateLimiterService(redis, configuration);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
