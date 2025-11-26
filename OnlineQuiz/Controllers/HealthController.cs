using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Services;

namespace OnlineQuiz.Controllers;

/// <summary>
/// Health check controller for monitoring API and database connectivity
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly SupabaseService _supabaseService;

    public HealthController(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// Health check endpoint to verify API and database connectivity
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            // Check database connectivity by performing a simple query
            var dbHealthy = await CheckDatabaseHealth();

            var healthStatus = new
            {
                status = dbHealthy ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "OnlineQuiz API",
                version = "1.0.0",
                checks = new
                {
                    api = new
                    {
                        status = "healthy",
                        message = "API is running"
                    },
                    database = new
                    {
                        status = dbHealthy ? "healthy" : "unhealthy",
                        message = dbHealthy ? "Database connection successful" : "Database connection failed"
                    }
                }
            };

            if (!dbHealthy)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);
            }

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            var errorStatus = new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "OnlineQuiz API",
                version = "1.0.0",
                error = ex.Message,
                checks = new
                {
                    api = new
                    {
                        status = "healthy",
                        message = "API is running"
                    },
                    database = new
                    {
                        status = "unhealthy",
                        message = "Database health check failed"
                    }
                }
            };

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorStatus);
        }
    }

    /// <summary>
    /// Simplified health check endpoint
    /// </summary>
    /// <returns>Simple health status</returns>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new
        {
            status = "healthy",
            message = "pong",
            timestamp = DateTime.UtcNow
        });
    }

    private async Task<bool> CheckDatabaseHealth()
    {
        try
        {
            // Perform a lightweight query to check database connectivity
            // Using a simple count query on the users table
            var client = _supabaseService.GetClient();
            var result = await client.From<Models.User>()
                .Limit(1)
                .Get();

            return result != null && result.Models != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
