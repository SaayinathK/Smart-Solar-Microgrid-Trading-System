using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Middleware;
using SmartMicrogrid.API.Repositories.Implementation;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Implementation;
using SmartMicrogrid.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Optional machine-local settings are ignored by Git. Environment/CLI values still win.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Add Controllers with JSON Enum string converter
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure MongoDB Settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register MongoDbContext as Singleton
builder.Services.AddSingleton<MongoDbContext>();

// Register Repositories & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMicrogridRepository, MicrogridRepository>();
builder.Services.AddScoped<IEnergySlotRepository, EnergySlotRepository>();
builder.Services.AddScoped<IMicrogridService, MicrogridService>();
builder.Services.AddHttpClient("OpenStreetMapGeocoding", client =>
    client.Timeout = TimeSpan.FromSeconds(8))
    // Addresses need not appear in HTTP request logs.
    .RemoveAllLoggers();
builder.Services.AddSingleton<IGeocodingService>(services => new OpenStreetMapGeocodingService(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("OpenStreetMapGeocoding"),
    services.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IEnergyCapacityService, EnergyCapacityService>();
builder.Services.AddScoped<IBatteryService, BatteryService>();
builder.Services.AddScoped<IEnergySlotService, EnergySlotService>();
builder.Services.AddScoped<IMicrogridDashboardService, MicrogridDashboardService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddHostedService<EnergySlotCleanupService>();
builder.Services.AddHostedService<ReservationExpiryService>();
builder.Services.AddSingleton<JwtHelper>();

// Configure JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "SmartMicrogrid_Super_Secure_JWT_Secret_Key_2026_EAD_University_Project!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "SmartMicrogridAPI";
var audience = builder.Configuration["Jwt:Audience"] ?? "SmartMicrogridClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure CORS for Web Application & LAN Clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Swagger / OpenAPI with JWT Authorization Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Microgrid Energy Management API",
        Version = "v1",
        Description = "RESTful API Backend for Smart Microgrid Energy Management & Trading System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1Ni...\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Migrate legacy verifier accounts before typed User queries deserialize roles.
using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await DbSeeder.MigrateLegacyRolesAsync(mongoContext);

    // Drop and reseed if --reseed flag is passed
    if (args.Contains("--reseed"))
    {
        Console.WriteLine("🔄 --reseed flag detected. Dropping SmartMicrogridDB...");
        var client = new MongoDB.Driver.MongoClient(
            builder.Configuration.GetSection("MongoDB")["ConnectionString"] ?? "mongodb://localhost:27017");
        client.DropDatabase(
            builder.Configuration.GetSection("MongoDB")["DatabaseName"] ?? "SmartMicrogridDB");
        Console.WriteLine("✅ Database dropped. Re-seeding...");
    }

    await DbSeeder.SeedDefaultUsersAsync(mongoContext);
}

// Global Exception Handler Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Enable Swagger in Development & Staging
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Microgrid API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
