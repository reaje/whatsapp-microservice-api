using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WhatsApp.API.Middleware;
using WhatsApp.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Infrastructure (Database, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Supabase Client
builder.Services.AddSupabaseClient(builder.Configuration);

// Add OpenAPI for Scalar
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "WhatsApp Microservice API",
            Version = "v1",
            Description = "Microserviço de comunicação WhatsApp Multi-Tenant com Provider Baileys",
            Contact = new()
            {
                Name = "Ventry Team",
                Email = "dev@ventry.com"
            }
        };

        // Add security scheme for X-Client-Id
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSecurityScheme>();
        document.Components.SecuritySchemes["X-Client-Id"] = new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Description = "Client ID header for multi-tenant authentication",
            Name = "X-Client-Id",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header
        };

        // Add security requirement
        document.SecurityRequirements = new List<Microsoft.OpenApi.Models.OpenApiSecurityRequirement>
        {
            new()
            {
                [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new()
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "X-Client-Id"
                    }
                }] = Array.Empty<string>()
            }
        };

        return Task.CompletedTask;
    });
});
builder.Services.AddEndpointsApiExplorer();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WhatsApp.Infrastructure.Data.SupabaseContext>()
    .AddCheck<WhatsApp.Infrastructure.HealthChecks.BaileysServiceHealthCheck>(
        "baileys_service",
        tags: new[] { "baileys", "service" });

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "whatsapp-microservice";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "whatsapp-frontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Baileys Service Hosted Service
if (builder.Configuration.GetValue("BaileysService:AutoStart", true))
{
    builder.Services.AddHostedService<WhatsApp.Infrastructure.HostedServices.BaileysServiceHostedService>();
}

var app = builder.Build();

// Apply EF Core Migrations and Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");

        var context = services.GetRequiredService<WhatsApp.Infrastructure.Data.SupabaseContext>();
        await context.Database.MigrateAsync();

        logger.LogInformation("Database migrations applied successfully");

        // Seed database with test data
        await SeedDatabase(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}

// Configure the HTTP request pipeline.
app.MapOpenApi();

// Use Scalar for API documentation (available in all environments)
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("WhatsApp Microservice API")
        .WithTheme(Scalar.AspNetCore.ScalarTheme.Purple)
        .WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient);
});

app.UseHttpsRedirection();

app.UseRouting();

// CORS deve vir ANTES de Authentication
app.UseCors("AllowFrontend");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Multi-tenancy middleware (validates X-Client-Id header)
app.UseTenantMiddleware();

app.MapControllers();

// Map Health Checks
app.MapHealthChecks("/health");

app.Run();

// Seed database with test data
static async Task SeedDatabase(WhatsApp.Infrastructure.Data.SupabaseContext context, ILogger logger)
{
    logger.LogInformation("Checking if database needs seeding...");

    // Clear change tracker to avoid conflicts
    context.ChangeTracker.Clear();

    // Check if tenant exists (use AsNoTracking to query database directly)
    var tenantExists = await context.Tenants.AsNoTracking().AnyAsync(t => t.ClientId == "a4876b9d-8ce5-4b67-ab69-c04073ce2f80");

    if (!tenantExists)
    {
        WhatsApp.Core.Entities.Tenant? tenant = null;

        try
        {
            logger.LogInformation("Seeding test tenant...");

            var settingsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                max_sessions = 10,
                max_messages_per_day = 1000,
                webhook_url = (string?)null,
                features = new
                {
                    ai_enabled = true,
                    media_enabled = true,
                    location_enabled = true
                }
            });

            tenant = new WhatsApp.Core.Entities.Tenant
            {
                Id = Guid.Parse("b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d"),
                ClientId = "a4876b9d-8ce5-4b67-ab69-c04073ce2f80",
                Name = "Test Tenant for Development",
                Settings = System.Text.Json.JsonDocument.Parse(settingsJson),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();

            logger.LogInformation("Test tenant created: a4876b9d-8ce5-4b67-ab69-c04073ce2f80");
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            logger.LogInformation("Test tenant already exists (duplicate key), skipping...");

            // Detach the tenant entity that failed to save
            if (tenant != null)
            {
                context.Entry(tenant).State = EntityState.Detached;
            }
        }
    }
    else
    {
        logger.LogInformation("Test tenant already exists");
    }

    // Check if users exist (use AsNoTracking to query database directly)
    var adminExists = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "admin@test.com");

    if (adminExists == null)
    {
        WhatsApp.Core.Entities.User? adminUser = null;
        WhatsApp.Core.Entities.User? regularUser = null;

        try
        {
            logger.LogInformation("Seeding test users...");

            var tenantId = Guid.Parse("b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d");

            adminUser = new WhatsApp.Core.Entities.User
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c7d-9e8f7a6b5c4d"),
                TenantId = tenantId,
                Email = "admin@test.com",
                PasswordHash = "$2a$11$IqIurrw1jkWsZMKQsAnmNOe7me3OV6NuciDRC8tzkKNgHg2HbRXN2", // Admin@123
                FullName = "Test Admin User",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            regularUser = new WhatsApp.Core.Entities.User
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-5b6c-9d8e-0f9a8b7c6d5e"),
                TenantId = tenantId,
                Email = "user@test.com",
                PasswordHash = "$2a$11$3ui9y1U/cR6XMpCm1xz60uYOqQYLB9i5rKwRI9w3N7kDTX0xRadju", // User@123
                FullName = "Test Regular User",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            context.Users.Add(regularUser);
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Test users created:");
            logger.LogInformation("  - Admin: admin@test.com / Admin@123");
            logger.LogInformation("  - User:  user@test.com / User@123");
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            logger.LogInformation("Test users already exist (duplicate key), skipping...");

            // Detach the user entities that failed to save
            if (adminUser != null)
            {
                context.Entry(adminUser).State = EntityState.Detached;
            }
            if (regularUser != null)
            {
                context.Entry(regularUser).State = EntityState.Detached;
            }
        }
    }
    else
    {
        // Update password hashes if they're different
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.com");
        var regularUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "user@test.com");

        bool updated = false;

        if (adminUser != null && adminUser.PasswordHash != "$2a$11$IqIurrw1jkWsZMKQsAnmNOe7me3OV6NuciDRC8tzkKNgHg2HbRXN2")
        {
            adminUser.PasswordHash = "$2a$11$IqIurrw1jkWsZMKQsAnmNOe7me3OV6NuciDRC8tzkKNgHg2HbRXN2";
            updated = true;
        }

        if (regularUser != null && regularUser.PasswordHash != "$2a$11$3ui9y1U/cR6XMpCm1xz60uYOqQYLB9i5rKwRI9w3N7kDTX0xRadju")
        {
            regularUser.PasswordHash = "$2a$11$3ui9y1U/cR6XMpCm1xz60uYOqQYLB9i5rKwRI9w3N7kDTX0xRadju";
            updated = true;
        }

        if (updated)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("✅ Test users password hashes updated");
        }
        else
        {
            logger.LogInformation("✅ Test users already exist with correct hashes");
        }
    }
}

// Make the implicit Program class public for integration testing
public partial class Program { }
