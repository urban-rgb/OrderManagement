using OrderManagement.Data;
using OrderManagement.Services;
using OrderManagement.Middleware;
using Microsoft.EntityFrameworkCore;
using Mapster;
using MapsterMapper;
using OrderManagement;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(OrderMapper).Assembly);

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, Mapper>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = builder.Configuration["RedisOptions:InstanceName"];
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }