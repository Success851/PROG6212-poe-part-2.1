using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDayTestAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay Test API",
        Version = "v1",
        Description = "Test version of the RaceDay Event Management API"
    });
});

// Add database
builder.Services.AddDbContext<RaceDayTestDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("RaceDayTestConnection")
    ));

// Add session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy =
       Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "RaceDay Test API v1");
    });
}

app.UseHttpsRedirection();

// IMPORTANT: Session middleware must run before the controllers.
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();
