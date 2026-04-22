using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Services;
using OnlineQuiz.Attributes;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth("X-ESP32-API-Key", "Biometric:ESP32:ApiKey")]
    public class ESP32Controller : ControllerBase
    {
        private readonly IESP32Service _esp32ServiceBase;
        private readonly HttpESP32Service? _httpEsp32Service;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ESP32Controller> _logger;

        public ESP32Controller(IESP32Service esp32Service, IConfiguration configuration, ILogger<ESP32Controller> logger)
        {
            _esp32ServiceBase = esp32Service;
            _httpEsp32Service = esp32Service as HttpESP32Service;
            _configuration = configuration;
            _logger = logger;
        }



        [HttpGet("poll")]
        public IActionResult Poll()
        {

            if (_httpEsp32Service == null)
            {
                return StatusCode(500, new { message = "HttpESP32Service is not configured. Is Mock mode on?" });
            }

            var cmd = _httpEsp32Service.PopCommand();
            if (cmd != null)
            {
                return Ok(cmd);
            }

            // Just a heartbeat
            _httpEsp32Service.Heartbeat();
            return NoContent();
        }

        public class ESP32ResultPayload
        {
            public string CommandType { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int UserId { get; set; }
            public int SlotId { get; set; }
        }

        [HttpPost("result")]
        public IActionResult SubmitResult([FromBody] ESP32ResultPayload payload)
        {

            if (_httpEsp32Service == null)
            {
                return StatusCode(500, new { message = "HttpESP32Service is not configured." });
            }

            var responseDto = new ESP32ResponseDto
            {
                Success = payload.Success,
                Message = payload.Message,
                UserId = payload.UserId,
                SlotId = payload.SlotId,
                Timestamp = DateTime.UtcNow
            };

            _httpEsp32Service.SubmitResult(responseDto, payload.CommandType);

            return Ok(new { success = true });
        }
    }
}
