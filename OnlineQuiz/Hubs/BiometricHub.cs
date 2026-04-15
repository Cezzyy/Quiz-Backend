using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Hubs
{
    [Authorize]
    public class BiometricHub : Hub
    {
        private readonly IBiometricService _biometricService;
        private readonly ILogger<BiometricHub> _logger;

        public BiometricHub(IBiometricService biometricService, ILogger<BiometricHub> logger)
        {
            _biometricService = biometricService;
            _logger = logger;
        }

        // =====================================================
        // CONNECTION MANAGEMENT
        // =====================================================

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            _logger.LogInformation("BiometricHub: User {UserId} connected with connection ID {ConnectionId}", userId, Context.ConnectionId);
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            _logger.LogInformation("BiometricHub: User {UserId} disconnected with connection ID {ConnectionId}", userId, Context.ConnectionId);
            
            await base.OnDisconnectedAsync(exception);
        }

        // =====================================================
        // CLIENT → SERVER METHODS
        // =====================================================

        /// <summary>
        /// Request fingerprint enrollment for a user
        /// </summary>
        public async Task RequestEnrollment(int userId)
        {
            try
            {
                var currentUserId = int.Parse(Context.User?.FindFirst("userId")?.Value ?? "0");
                _logger.LogInformation("BiometricHub: Enrollment requested for user {UserId} by {CurrentUserId}", userId, currentUserId);

                var result = await _biometricService.StartEnrollmentAsync(userId, currentUserId);

                if (result.Success)
                {
                    await Clients.All.SendAsync("EnrollmentStarted", new
                    {
                        userId = userId,
                        slotId = result.SlotId,
                        message = result.Message
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("EnrollmentFailed", new
                    {
                        userId = userId,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BiometricHub: Error requesting enrollment for user {UserId}", userId);
                await Clients.Caller.SendAsync("EnrollmentFailed", new
                {
                    userId = userId,
                    message = "An error occurred while starting enrollment"
                });
            }
        }

        /// <summary>
        /// Request fingerprint verification for a user
        /// </summary>
        public async Task RequestVerification(int userId, int? quizId = null)
        {
            try
            {
                _logger.LogInformation("BiometricHub: Verification requested for user {UserId}", userId);

                var result = await _biometricService.StartVerificationAsync(userId, quizId);

                if (result.Success)
                {
                    await Clients.All.SendAsync("VerificationStarted", new
                    {
                        userId = userId,
                        quizId = quizId,
                        message = result.Message
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("VerificationFailed", new
                    {
                        userId = userId,
                        matched = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BiometricHub: Error requesting verification for user {UserId}", userId);
                await Clients.Caller.SendAsync("VerificationFailed", new
                {
                    userId = userId,
                    matched = false,
                    message = "An error occurred while starting verification"
                });
            }
        }

        /// <summary>
        /// Cancel current biometric operation
        /// </summary>
        public async Task CancelOperation()
        {
            try
            {
                _logger.LogInformation("BiometricHub: Cancel operation requested");

                var success = await _biometricService.CancelCurrentOperationAsync();

                await Clients.All.SendAsync("OperationCancelled", new
                {
                    success = success,
                    message = success ? "Operation cancelled" : "Failed to cancel operation"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BiometricHub: Error cancelling operation");
                await Clients.Caller.SendAsync("Error", new
                {
                    message = "An error occurred while cancelling operation"
                });
            }
        }

        /// <summary>
        /// Get current device status
        /// </summary>
        public async Task GetDeviceStatus()
        {
            try
            {
                var status = await _biometricService.GetDeviceStatusAsync();

                await Clients.Caller.SendAsync("DeviceStatus", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BiometricHub: Error getting device status");
                await Clients.Caller.SendAsync("Error", new
                {
                    message = "An error occurred while getting device status"
                });
            }
        }

        // =====================================================
        // SERVER → CLIENT EVENTS (called by BiometricListenerService)
        // =====================================================
        // These methods are called by the background service to broadcast events:
        // - EnrollmentStarted
        // - EnrollmentProgress (e.g., "Place finger again")
        // - EnrollmentCompleted
        // - EnrollmentFailed
        // - VerificationStarted
        // - VerificationCompleted
        // - VerificationFailed
        // - DeviceStatusChanged
    }
}
