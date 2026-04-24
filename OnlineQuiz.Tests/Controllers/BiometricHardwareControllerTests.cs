using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // Fake IESP32Service
    // ─────────────────────────────────────────────────────────────────────────
    internal class FakeESP32Service : IESP32Service
    {
        public bool ProcessEventThrows { get; set; } = false;
        public ESP32HardwareEventDto? LastProcessedEvent { get; private set; }

        public Task ProcessHardwareEventAsync(ESP32HardwareEventDto eventData)
        {
            if (ProcessEventThrows)
                throw new InvalidOperationException("Hardware processing failed");
            LastProcessedEvent = eventData;
            return Task.CompletedTask;
        }

        public Task<BiometricStatusDto> GetDeviceStatusAsync()
            => Task.FromResult(new BiometricStatusDto { IsConnected = true, CurrentMode = "Idle" });

        public Task<ESP32ResponseDto> SendEnrollCommandAsync(int slotId, int userId)
            => Task.FromResult(new ESP32ResponseDto { Success = true, SlotId = slotId, UserId = userId });

        public Task<ESP32ResponseDto> SendVerifyCommandAsync(int slotId, int userId)
            => Task.FromResult(new ESP32ResponseDto { Success = true, SlotId = slotId, UserId = userId });

        public Task<ESP32ResponseDto> CancelOperationAsync()
            => Task.FromResult(new ESP32ResponseDto { Success = true });

        public Task<bool> ConnectAsync() => Task.FromResult(true);
        public Task DisconnectAsync() => Task.CompletedTask;
        public bool IsConnected => true;

        public event EventHandler<ESP32ResponseDto>? OnEnrollmentCompleted;
        public event EventHandler<ESP32ResponseDto>? OnVerificationCompleted;
        public event EventHandler<string>? OnDeviceStatusChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests
    // Note: [ApiKeyAuth] is an action filter — it does NOT run when calling
    // controller methods directly in unit tests. We test the controller logic
    // only. API key validation is tested separately as an attribute concern.
    // ─────────────────────────────────────────────────────────────────────────
    [Collection("MapsterWarmup")]
    public class BiometricHardwareControllerTests
    {
        private static BiometricHardwareController CreateController(FakeESP32Service? esp32 = null)
        {
            var controller = new BiometricHardwareController(
                esp32 ?? new FakeESP32Service(),
                NullLogger<BiometricHardwareController>.Instance);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        // ── ReceiveHardwareEvent ──────────────────────────────────────────

        [Fact]
        public async Task ReceiveHardwareEvent_ReturnsOk_WhenValidEvent()
        {
            var fake = new FakeESP32Service();
            var controller = CreateController(fake);

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = 5,
                DeviceId = "ESP32-001",
                Status = "Success"
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            // Verify the event was forwarded to the service
            Assert.NotNull(fake.LastProcessedEvent);
            Assert.Equal(5, fake.LastProcessedEvent!.FingerprintId);
            Assert.Equal("ESP32-001", fake.LastProcessedEvent.DeviceId);
        }

        [Fact]
        public async Task ReceiveHardwareEvent_ReturnsBadRequest_WhenFingerprintIdIsZero()
        {
            var controller = CreateController();

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = 0,   // invalid — must be 1-127
                DeviceId = "ESP32-001",
                Status = "Success"
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task ReceiveHardwareEvent_ReturnsBadRequest_WhenFingerprintIdExceeds127()
        {
            var controller = CreateController();

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = 128,  // invalid — max is 127
                DeviceId = "ESP32-001",
                Status = "Success"
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReceiveHardwareEvent_ReturnsBadRequest_WhenStatusIsEmpty()
        {
            var controller = CreateController();

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = 5,
                DeviceId = "ESP32-001",
                Status = ""   // invalid — required
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReceiveHardwareEvent_ReturnsOk_WhenStatusIsFailed()
        {
            // A "Failed" status from hardware is still a valid event — not a bad request
            var fake = new FakeESP32Service();
            var controller = CreateController(fake);

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = 3,
                DeviceId = "ESP32-001",
                Status = "Failed",
                ErrorMessage = "Finger not detected"
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(fake.LastProcessedEvent);
        }

        [Fact]
        public async Task ReceiveHardwareEvent_Returns500_WhenServiceThrows()
        {
            var fake = new FakeESP32Service { ProcessEventThrows = true };
            var controller = CreateController(fake);

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = 5,
                DeviceId = "ESP32-001",
                Status = "Success"
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Theory]
        [InlineData(1)]    // minimum valid
        [InlineData(64)]   // middle
        [InlineData(127)]  // maximum valid
        public async Task ReceiveHardwareEvent_ReturnsOk_ForAllValidFingerprintIds(int fingerprintId)
        {
            var controller = CreateController();

            var eventData = new ESP32HardwareEventDto
            {
                FingerprintId = fingerprintId,
                DeviceId = "ESP32-001",
                Status = "Success"
            };

            var result = await controller.ReceiveHardwareEvent(eventData);

            Assert.IsType<OkObjectResult>(result);
        }

        // ── Ping ──────────────────────────────────────────────────────────

        [Fact]
        public void Ping_ReturnsOk_WithReadyMessage()
        {
            var controller = CreateController();

            var result = controller.Ping();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // ── GetStatus ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetStatus_ReturnsOk_WithDeviceStatus()
        {
            var controller = CreateController();

            var result = await controller.GetStatus();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricStatusDto>(ok.Value);
            Assert.True(dto.IsConnected);
            Assert.Equal("Idle", dto.CurrentMode);
        }
    }
}
