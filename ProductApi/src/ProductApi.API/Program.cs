using System.Threading.RateLimiting;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProductApi.API.Extensions;
using ProductApi.API.Middleware;
using ProductApi.Infrastructure.Data;
using ProductApi.Infrastructure.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Structured logging (Serilog) ----------
Log.Logger = SerilogConfigurator.BuildConfiguration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

// ---------- Services ----------
builder.Services.AddControllers();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiVersioningSupport();

// Swagger with JWT support
builder.Services.AddSwaggerWithJwt();

builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddResponseCompression();

// Simple IP-based rate limiting (protects the API from abuse)
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));

builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// ---------- Middleware pipeline ----------

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger enabled for Docker and local environments
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
});

app.UseSerilogRequestLogging();

app.UseIpRateLimiting();

app.UseResponseCompression();

// Enable HTTPS redirect only when HTTPS is configured
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCorsPolicy");

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "X-Content-Type-Options",
        "nosniff");

    context.Response.Headers.Append(
        "X-Frame-Options",
        "DENY");

    context.Response.Headers.Append(
        "Referrer-Policy",
        "no-referrer");

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// ---------- Apply EF Core migrations automatically ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();
}

// ---------- Run Application ----------
try
{
    Log.Information("Starting Product API");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Product API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory-based integration tests
public partial class Program { }