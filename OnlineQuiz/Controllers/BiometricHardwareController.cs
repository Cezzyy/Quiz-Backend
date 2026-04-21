using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Attributes;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    /// <summary>
    /// Controller for receiving events from ESP32 hardware
    /// This endpoint is called by the ESP32 device, not by frontend clients
    /// SECURITY: Protected by API key authentication (X-API-Key header)
    /// </summary>
    [ApiController]
    [Route("api/biometric/hardware")]
    [ApiKeyAuth] // Require API key for all endpoints in this controller
    public class BiometricHardwareController : ControllerBase
    {
        private readonly IESP32Service _esp32Service;
        private readonly ILogger<BiometricHardwareController> _logger;

        public BiometricHardwareController(
            IESP32Service esp32Service,
            ILogger<BiometricHardwareController> logger)
        {
            _esp32Service = esp32Service;
            _logger = logger;
        }

        /// <summary>
        /// Receive events from ESP32 hardware
        /// Called by ESP32 when enrollment or verification completes
        /// SECURITY: Requires X-API-Key header for authentication
        /// </summary>
        /// <param name="eventData">Event data from ESP32</param>
        /// <returns>Acknowledgment</returns>
        [HttpPost("event")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReceiveHardwareEvent([FromBody] ESP32HardwareEventDto eventData)
        {
            try
            {
                _logger.LogInformation(
                    "Received hardware event from {DeviceId}: FingerprintId={FingerprintId}, Status={Status}", 
                    eventData.DeviceId,
                    eventData.FingerprintId, 
                    eventData.Status);

                // Validate input
                if (eventData.FingerprintId <= 0 || eventData.FingerprintId > 127)
                {
                    return BadRequest(new { message = "Invalid fingerprint ID. Must be between 1 and 127." });
                }

                if (string.IsNullOrEmpty(eventData.Status))
                {
                    return BadRequest(new { message = "Status is required." });
                }

                // Process the event through ESP32Service
                await _esp32Service.ProcessHardwareEventAsync(eventData);

                return Ok(new { 
                    message = "Event received successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hardware event from {DeviceId}", eventData.DeviceId);
                return StatusCode(500, new { message = "Error processing event" });
            }
        }

        /// <summary>
        /// Health check endpoint for ESP32
        /// ESP32 can call this to verify backend is reachable
        /// SECURITY: Requires X-API-Key header for authentication
        /// </summary>
        /// <returns>Status message</returns>
        [HttpGet("ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Ping()
        {
            _logger.LogDebug("Ping received from ESP32 device");
            
            return Ok(new { 
                message = "Backend is ready", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Get current device status
        /// SECURITY: Requires X-API-Key header for authentication
        /// </summary>
        /// <returns>Device status</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(BiometricStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BiometricStatusDto>> GetStatus()
        {
            try
            {
                var status = await _esp32Service.GetDeviceStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status");
                return StatusCode(500, new { message = "Error getting device status" });
            }
        }
    }
}
