using Microsoft.Extensions.Logging.Abstractions;
using OnlineQuiz.DTOs;
using OnlineQuiz.Services;
using Xunit;

namespace OnlineQuiz.Tests.Services
{
    /// <summary>
    /// Tests for HttpESP32Service — pure in-memory logic, no hardware required.
    /// Tests the command queue, heartbeat/connection tracking, and result processing.
    /// </summary>
    [Collection("MapsterWarmup")]
    public class HttpESP32ServiceTests
    {
        private static HttpESP32Service Create()
            => new HttpESP32Service(NullLogger<HttpESP32Service>.Instance);

        // ── IsConnected / Heartbeat ───────────────────────────────────────

        [Fact]
        public void IsConnected_ReturnsFalse_Initially()
        {
            var svc = Create();
            Assert.False(svc.IsConnected);
        }

        [Fact]
        public void IsConnected_ReturnsTrue_AfterHeartbeat()
        {
            var svc = Create();
            svc.Heartbeat();
            Assert.True(svc.IsConnected);
        }

        [Fact]
        public async Task IsConnected_ReturnsTrue_AfterConnect()
        {
            var svc = Create();
            var result = await svc.ConnectAsync();
            Assert.True(result);
            Assert.True(svc.IsConnected);
        }

        [Fact]
        public async Task IsConnected_ReturnsFalse_AfterDisconnect()
        {
            var svc = Create();
            await svc.ConnectAsync();
            await svc.DisconnectAsync();
            Assert.False(svc.IsConnected);
        }

        [Fact]
        public void Heartbeat_FiresStatusChanged_WhenFirstConnect()
        {
            var svc = Create();
            string? receivedStatus = null;
            svc.OnDeviceStatusChanged += (_, s) => receivedStatus = s;

            svc.Heartbeat();

            Assert.Equal("Connected", receivedStatus);
        }

        [Fact]
        public void Heartbeat_DoesNotFireAgain_WhenAlreadyConnected()
        {
            var svc = Create();
            int eventCount = 0;
            svc.OnDeviceStatusChanged += (_, _) => eventCount++;

            svc.Heartbeat(); // first — fires Connected
            svc.Heartbeat(); // second — already connected, no event

            Assert.Equal(1, eventCount);
        }

        // ── SendEnrollCommandAsync / PopCommand ───────────────────────────

        [Fact]
        public async Task SendEnrollCommand_QueuesCommand_AndReturnsSuccess()
        {
            var svc = Create();
            var response = await svc.SendEnrollCommandAsync(slotId: 3, userId: 42);

            Assert.True(response.Success);
            Assert.Equal(3, response.SlotId);
            Assert.Equal(42, response.UserId);
        }

        [Fact]
        public async Task PopCommand_ReturnsEnrollCommand_AfterSendEnroll()
        {
            var svc = Create();
            await svc.SendEnrollCommandAsync(slotId: 5, userId: 10);

            var cmd = svc.PopCommand();

            Assert.NotNull(cmd);
            Assert.Equal("ENROLL", cmd!.CommandType);
            Assert.Equal(5, cmd.SlotId);
            Assert.Equal(10, cmd.UserId);
        }

        [Fact]
        public async Task PopCommand_ReturnsNull_WhenNoCommandQueued()
        {
            var svc = Create();
            await svc.ConnectAsync();

            var cmd = svc.PopCommand();

            Assert.Null(cmd);
        }

        [Fact]
        public async Task PopCommand_ClearsCommand_AfterFirstPop()
        {
            var svc = Create();
            await svc.SendEnrollCommandAsync(slotId: 1, userId: 1);

            svc.PopCommand(); // first pop — gets the command
            var second = svc.PopCommand(); // second pop — should be null

            Assert.Null(second);
        }

        // ── SendVerifyCommandAsync ────────────────────────────────────────

        [Fact]
        public async Task SendVerifyCommand_QueuesVerifyCommand()
        {
            var svc = Create();
            await svc.SendVerifyCommandAsync(slotId: 7, userId: 55);

            var cmd = svc.PopCommand();

            Assert.NotNull(cmd);
            Assert.Equal("VERIFY", cmd!.CommandType);
            Assert.Equal(7, cmd.SlotId);
            Assert.Equal(55, cmd.UserId);
        }

        [Fact]
        public async Task SendVerifyCommand_OverwritesPreviousCommand()
        {
            var svc = Create();
            await svc.SendEnrollCommandAsync(slotId: 1, userId: 1);
            await svc.SendVerifyCommandAsync(slotId: 2, userId: 2); // overwrites

            var cmd = svc.PopCommand();

            Assert.Equal("VERIFY", cmd!.CommandType);
        }

        // ── CancelOperationAsync ──────────────────────────────────────────

        [Fact]
        public async Task CancelOperation_ClearsQueuedCommand()
        {
            var svc = Create();
            await svc.SendEnrollCommandAsync(slotId: 3, userId: 1);
            await svc.CancelOperationAsync();

            var cmd = svc.PopCommand();

            Assert.Null(cmd);
        }

        [Fact]
        public async Task CancelOperation_ReturnsSuccess()
        {
            var svc = Create();
            var result = await svc.CancelOperationAsync();
            Assert.True(result.Success);
        }

        [Fact]
        public async Task CancelOperation_FiresIdleStatusEvent()
        {
            var svc = Create();
            string? status = null;
            svc.OnDeviceStatusChanged += (_, s) => status = s;

            await svc.CancelOperationAsync();

            Assert.Equal("Idle", status);
        }

        // ── SubmitResult ──────────────────────────────────────────────────

        [Fact]
        public async Task SubmitResult_FiresEnrollmentCompleted_ForEnrollCommand()
        {
            var svc = Create();
            ESP32ResponseDto? received = null;
            svc.OnEnrollmentCompleted += (_, r) => received = r;

            await svc.SendEnrollCommandAsync(slotId: 3, userId: 1);
            svc.SubmitResult(new ESP32ResponseDto { Success = true, SlotId = 3, UserId = 1 }, "ENROLL");

            Assert.NotNull(received);
            Assert.True(received!.Success);
        }

        [Fact]
        public async Task SubmitResult_FiresVerificationCompleted_ForVerifyCommand()
        {
            var svc = Create();
            ESP32ResponseDto? received = null;
            svc.OnVerificationCompleted += (_, r) => received = r;

            await svc.SendVerifyCommandAsync(slotId: 2, userId: 5);
            svc.SubmitResult(new ESP32ResponseDto { Success = true, SlotId = 2, UserId = 5 }, "VERIFY");

            Assert.NotNull(received);
            Assert.True(received!.Success);
        }

        [Fact]
        public void SubmitResult_ResetsMode_ToIdle()
        {
            var svc = Create();
            svc.SubmitResult(new ESP32ResponseDto { Success = true }, "ENROLL");

            // After submit, status should be Idle
            var statusTask = svc.GetDeviceStatusAsync();
            var status = statusTask.Result;
            Assert.Equal("Idle", status.CurrentMode);
        }

        // ── GetDeviceStatusAsync ──────────────────────────────────────────

        [Fact]
        public async Task GetDeviceStatus_ReflectsCurrentMode_AfterEnrollCommand()
        {
            var svc = Create();
            await svc.SendEnrollCommandAsync(slotId: 1, userId: 1);

            var status = await svc.GetDeviceStatusAsync();

            Assert.Equal("Enrollment", status.CurrentMode);
            Assert.Equal(1, status.ActiveUserId);
        }

        [Fact]
        public async Task GetDeviceStatus_ReflectsCurrentMode_AfterVerifyCommand()
        {
            var svc = Create();
            await svc.SendVerifyCommandAsync(slotId: 2, userId: 7);

            var status = await svc.GetDeviceStatusAsync();

            Assert.Equal("Verification", status.CurrentMode);
            Assert.Equal(7, status.ActiveUserId);
        }
    }
}
