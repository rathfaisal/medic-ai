using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using MedicAI.Api.Data;
using MedicAI.Api.MachineLearning;
using MedicAI.Api.Middleware;
using MedicAI.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MEDIC-AI API",
        Version = "v1",
        Description = "Patient pre-triage and clinic management FYP prototype. Preliminary urgency guidance only; no diagnosis."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
}

builder.Services.AddDbContext<MedicAiDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key must be configured.");

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleText = context.Principal?.FindFirstValue(ClaimTypes.Role);

                if (!int.TryParse(userIdText, out var userId))
                {
                    context.Fail("The access token does not contain a valid user identifier.");
                    return;
                }

                try
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<MedicAiDbContext>();
                    var account = await dbContext.Users
                        .AsNoTracking()
                        .Where(x => x.Id == userId)
                        .Select(x => new { x.Status, x.Role })
                        .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                    if (account is null || account.Status != MedicAI.Api.Domain.UserStatus.Active)
                    {
                        context.Fail("The account is no longer active.");
                        return;
                    }

                    if (!string.Equals(account.Role.ToString(), roleText, StringComparison.Ordinal))
                    {
                        context.Fail("The account role has changed. Sign in again.");
                    }
                }
                catch
                {
                    context.Fail("The account could not be validated because the database is temporarily unavailable.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var configuredCorsOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?? "http://localhost:3000,http://localhost:5173";

var allowedCorsOrigins = configuredCorsOrigins
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

builder.Services.AddScoped<
    Microsoft.AspNetCore.Identity.IPasswordHasher<MedicAI.Api.Domain.User>,
    Microsoft.AspNetCore.Identity.PasswordHasher<MedicAI.Api.Domain.User>>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRedFlagRuleEngine, RedFlagRuleEngine>();
builder.Services.AddScoped<ITriageService, TriageService>();
builder.Services.AddScoped<IQueueService, QueueService>();

builder.Services.AddSingleton<MlTriageModelService>();
builder.Services.AddHostedService<EmergencyAlertEscalationWorker>();

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("WebClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", async (
    MedicAiDbContext dbContext,
    MlTriageModelService modelService,
    CancellationToken cancellationToken) =>
{
    var databaseAvailable = false;
    string? databaseStatus = null;

    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeout.CancelAfter(TimeSpan.FromSeconds(5));

    try
    {
        databaseAvailable = await dbContext.Database.CanConnectAsync(timeout.Token);
        databaseStatus = databaseAvailable ? "Connected" : "Unavailable";
    }
    catch (OperationCanceledException)
    {
        databaseStatus = "Timed out";
    }
    catch
    {
        databaseStatus = "Unavailable";
    }

    var payload = new
    {
        status = databaseAvailable
            ? modelService.IsAvailable ? "Healthy" : "Degraded"
            : "Starting/Degraded",
        service = "MEDIC-AI API",
        databaseAvailable,
        databaseStatus,
        mlModelAvailable = modelService.IsAvailable,
        mlModelVersion = modelService.ModelVersionName,
        manualReviewFallbackAvailable = true,
        initializationRunsInBackground = true,
        utcTime = DateTimeOffset.UtcNow
    };

    return Results.Ok(payload);
});

app.MapFallbackToFile("index.html");

/*
 * IMPORTANT FOR SHARED HOSTING
 * ----------------------------
 * Database seeding and ML.NET model initialisation used to run BEFORE app.Run().
 * On MonsterASP this could make every browser request wait while the database
 * retried and the model initialised.
 *
 * The application now starts serving HTTP immediately. Initialisation happens
 * in the background after IIS/Kestrel reports that the app has started.
 */
var startupLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("StartupInitialization");

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        var stoppingToken = app.Lifetime.ApplicationStopping;

        try
        {
            startupLogger.LogInformation("Starting MEDIC-AI database initialisation in the background.");
            await DatabaseSeeder.InitialiseAsync(app.Services, stoppingToken);
            startupLogger.LogInformation("MEDIC-AI database initialisation completed.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex,
                "Database initialisation failed. The website will remain online, but database functions may be unavailable.");
        }

        try
        {
            startupLogger.LogInformation("Starting ML.NET model initialisation in the background.");
            var modelService = app.Services.GetRequiredService<MlTriageModelService>();
            await modelService.InitializeAsync(app.Services, stoppingToken);
            startupLogger.LogInformation(
                "ML.NET initialisation finished. Available: {Available}. Version: {Version}.",
                modelService.IsAvailable,
                modelService.ModelVersionName);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex,
                "ML.NET model initialisation failed. Manual review fallback remains available.");
        }
    });
});

app.Run();

public partial class Program { }
