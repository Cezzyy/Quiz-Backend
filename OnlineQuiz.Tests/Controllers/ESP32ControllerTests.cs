using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.Services;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    /// <summary>
    /// Tests for ESP32Controller.
    /// The controller casts IESP32Service to HttpESP32Service internally,
    /// so we pass a real HttpESP32Service instance (no hardware involved —
    /// it is pure in-memory state management).
    /// [ApiKeyAuth] is an action filter that does NOT run when calling
    /// controller methods directly in unit tests.
    /// </summary>
    [Collection("MapsterWarmup")]
    public class ESP32ControllerTests
    {
        private static (ESP32Controller controller, HttpESP32Service service) CreateController()
        {
            var service = new HttpESP32Service(NullLogger<HttpESP32Service>.Instance);
            var config = new ConfigurationBuilder().Build();
            var controller = new ESP32Controller(service, config, NullLogger<ESP32Controller>.Instance);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return (controller, service);
        }

        // ── Poll ──────────────────────────────────────────────────────────

        [Fact]
        public void Poll_ReturnsNoContent_WhenNoCommandQueued()
        {
            var (controller, _) = CreateController();
            var result = controller.Poll();
            // No command queued → heartbeat only → NoContent
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Poll_ReturnsOk_WithCommand_WhenCommandQueued()
        {
            var (controller, service) = CreateController();
            await service.SendEnrollCommandAsync(slotId: 3, userId: 10);

            var result = controller.Poll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var cmd = Assert.IsType<PendingCommand>(ok.Value);
            Assert.Equal("ENROLL", cmd.CommandType);
            Assert.Equal(3, cmd.SlotId);
            Assert.Equal(10, cmd.UserId);
        }

        [Fact]
        public void Poll_UpdatesConnectionStatus_ViaHeartbeat()
        {
            var (controller, service) = CreateController();
            Assert.False(service.IsConnected); // initially disconnected

            controller.Poll(); // triggers Heartbeat internally

            Assert.True(service.IsConnected);
        }

        [Fact]
        public async Task Poll_ClearsCommand_AfterReturning()
        {
            var (controller, service) = CreateController();
            await service.SendEnrollCommandAsync(slotId: 1, userId: 1);

            controller.Poll(); // first poll — returns command
            var second = controller.Poll(); // second poll — no command

            Assert.IsType<NoContentResult>(second);
        }

        // ── SubmitResult ──────────────────────────────────────────────────

        [Fact]
        public void SubmitResult_ReturnsOk_WhenValid()
        {
            var (controller, _) = CreateController();
            var payload = new ESP32Controller.ESP32ResultPayload
            {
                CommandType = "ENROLL",
                Success = true,
                Message = "Enrolled",
                UserId = 10,
                SlotId = 3
            };

            var result = controller.SubmitResult(payload);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public void SubmitResult_FiresEnrollmentCompleted_Event()
        {
            var (controller, service) = CreateController();
            ESP32ResponseDto? received = null;
            service.OnEnrollmentCompleted += (_, r) => received = r;

            controller.SubmitResult(new ESP32Controller.ESP32ResultPayload
            {
                CommandType = "ENROLL",
                Success = true,
                UserId = 5,
                SlotId = 2
            });

            Assert.NotNull(received);
            Assert.True(received!.Success);
            Assert.Equal(5, received.UserId);
        }

        [Fact]
        public void SubmitResult_FiresVerificationCompleted_Event()
        {
            var (controller, service) = CreateController();
            ESP32ResponseDto? received = null;
            service.OnVerificationCompleted += (_, r) => received = r;

            controller.SubmitResult(new ESP32Controller.ESP32ResultPayload
            {
                CommandType = "VERIFY",
                Success = false,
                Message = "No match",
                UserId = 7,
                SlotId = 4
            });

            Assert.NotNull(received);
            Assert.False(received!.Success);
        }

        [Fact]
        public async Task SubmitResult_ResetsMode_ToIdle()
        {
            var (controller, service) = CreateController();
            await service.SendEnrollCommandAsync(slotId: 1, userId: 1);

            controller.SubmitResult(new ESP32Controller.ESP32ResultPayload
            {
                CommandType = "ENROLL",
                Success = true,
                UserId = 1,
                SlotId = 1
            });

            var status = await service.GetDeviceStatusAsync();
            Assert.Equal("Idle", status.CurrentMode);
        }
    }
}
