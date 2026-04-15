using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Predixus.API.BackgroundJobs;
using Predixus.Application.Interfaces;
using Predixus.Application.Services;
using Predixus.Domain.Interfaces;
using Predixus.Infrastructure.Cache;
using Predixus.Infrastructure.ExternalServices;
using Predixus.Infrastructure.Persistence;
using Predixus.Infrastructure.Persistence.Repositories;
using Predixus.Infrastructure.Security;
using Predixus.API;
using Predixus.API.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Predixus API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey yapılandırması eksik.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Redis
var redisConnection = builder.Configuration["Redis:ConnectionString"]
    ?? throw new InvalidOperationException("Redis:ConnectionString yapılandırması eksik.");
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<RedisCacheService>();
builder.Services.AddSingleton<RedisRateLimiter>();

// HTTP Clients
var yahooBaseUrl = builder.Configuration["YahooFinance:BaseUrl"]
    ?? throw new InvalidOperationException("YahooFinance:BaseUrl yapılandırması eksik.");
builder.Services.AddHttpClient<IYahooFinanceClient, YahooFinanceClient>(
    c => c.BaseAddress = new Uri(yahooBaseUrl));

var mlBaseUrl = builder.Configuration["MlService:BaseUrl"]
    ?? throw new InvalidOperationException("MlService:BaseUrl yapılandırması eksik.");
var mlTimeout = builder.Configuration.GetValue<int>("MlService:TimeoutSeconds", 30);
builder.Services.AddHttpClient<IMlPredictionClient, MlPredictionClient>(c =>
{
    c.BaseAddress = new Uri(mlBaseUrl);
    c.Timeout = TimeSpan.FromSeconds(mlTimeout);
});

// DI Kayıtları
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockPriceRepository, StockPriceRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStockDataService, StockDataService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Background Jobs
builder.Services.AddHostedService<StockDataFetchJob>();

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Predixus API v1"));

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DataSeeder.SeedAsync(app.Services);

app.Run();
