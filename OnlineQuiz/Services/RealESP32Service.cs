using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using System.Collections.Concurrent;

namespace OnlineQuiz.Services
{
    /// <summary>
    /// Real ESP32 service implementation for hardware fingerprint sensor
    /// Manages communication with ESP32 device via HTTP events
    /// </summary>
    public class RealESP32Service : IESP32Service, IDisposable
    {
        private readonly ILogger<RealESP32Service> _logger;
        private readonly ConcurrentDictionary<int, PendingOperation> _pendingOperations;
        private readonly object _stateLock = new object();
        private volatile bool _isConnected;
        private DateTime _lastActivity; // Protected by _stateLock
        private readonly Timer _cleanupTimer;
        private const int OperationTimeoutMinutes = 5; // Timeout for pending operations
        private const int CleanupIntervalSeconds = 60; // Run cleanup every minute
        private const int DeviceStaleMinutes = 5; // Device considered stale after 5 minutes of inactivity

        public event EventHandler<ESP32ResponseDto>? OnEnrollmentCompleted;
        public event EventHandler<ESP32ResponseDto>? OnVerificationCompleted;
        public event EventHandler<string>? OnDeviceStatusChanged;

        /// <summary>
        /// Gets whether the device is connected AND not stale
        /// A device is considered stale if there's been no activity for 5+ minutes
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (!_isConnected)
                    return false;

                DateTime lastActivity;
                lock (_stateLock)
                {
                    lastActivity = _lastActivity;
                }

                var isStale = (DateTime.UtcNow - lastActivity).TotalMinutes > DeviceStaleMinutes;
                return !isStale;
            }
        }

        public bool IsCancelRequested { get; private set; }

        public RealESP32Service(ILogger<RealESP32Service> logger)
        {
            _logger = logger;
            _pendingOperations = new ConcurrentDictionary<int, PendingOperation>();
            _isConnected = true; // Assume connected (ESP32 will ping to confirm)
            lock (_stateLock)
            {
                _lastActivity = DateTime.UtcNow;
            }
            
            // Initialize cleanup timer to prevent memory leaks
            _cleanupTimer = new Timer(
                CleanupStaleOperations,
                null,
                TimeSpan.FromSeconds(CleanupIntervalSeconds),
                TimeSpan.FromSeconds(CleanupIntervalSeconds)
            );
            
            _logger.LogInformation("RealESP32Service initialized with automatic cleanup every {Interval}s", CleanupIntervalSeconds);
        }

        // =====================================================
        // COMMAND METHODS (Called by BiometricService)
        // =====================================================

        public async Task<ESP32ResponseDto> SendEnrollCommandAsync(int slotId, int userId)
        {
            try
            {
                _logger.LogInformation("Sending enroll command to ESP32: SlotId={SlotId}, UserId={UserId}", slotId, userId);

                // Store pending operation
                IsCancelRequested = false;
                var operation = new PendingOperation
                {
                    Type = "enroll",
                    SlotId = slotId,
                    UserId = userId,
                    StartedAt = DateTime.UtcNow
                };

                _pendingOperations[slotId] = operation;

                // In a real implementation with serial/TCP connection, you would send command here
                // For now, we rely on manual command entry via Serial Monitor
                _logger.LogWarning("MANUAL ACTION REQUIRED: Send 'E {SlotId}' command to ESP32 via Serial Monitor", slotId);

                lock (_stateLock)
                {
                    _lastActivity = DateTime.UtcNow;
                }

                return await Task.FromResult(new ESP32ResponseDto
                {
                    Success = true,
                    Message = $"Enrollment command queued for slot {slotId}. Send 'E {slotId}' to ESP32.",
                    SlotId = slotId,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending enroll command");
                return new ESP32ResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ESP32ResponseDto> SendVerifyCommandAsync(int slotId, int userId)
        {
            try
            {
                _logger.LogInformation("Sending verify command to ESP32: SlotId={SlotId}, UserId={UserId}", slotId, userId);

                // Store pending operation
                IsCancelRequested = false;
                var operation = new PendingOperation
                {
                    Type = "verify",
                    SlotId = slotId,
                    UserId = userId,
                    StartedAt = DateTime.UtcNow
                };

                _pendingOperations[slotId] = operation;

                // In a real implementation with serial/TCP connection, you would send command here
                _logger.LogWarning("MANUAL ACTION REQUIRED: Send 'V {SlotId}' command to ESP32 via Serial Monitor", slotId);

                lock (_stateLock)
                {
                    _lastActivity = DateTime.UtcNow;
                }

                return await Task.FromResult(new ESP32ResponseDto
                {
                    Success = true,
                    Message = $"Verification command queued for slot {slotId}. Send 'V {slotId}' to ESP32.",
                    SlotId = slotId,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verify command");
                return new ESP32ResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ESP32ResponseDto> CancelOperationAsync()
        {
            _logger.LogInformation("Cancelling current operation");
            
            IsCancelRequested = true;
            var count = _pendingOperations.Count;
            _pendingOperations.Clear();
            
            _logger.LogInformation("Cleared {Count} pending operations", count);

            return await Task.FromResult(new ESP32ResponseDto
            {
                Success = true,
                Message = $"Cancelled {count} pending operation(s)"
            });
        }

        public async Task<BiometricStatusDto> GetDeviceStatusAsync()
        {
            DateTime lastActivity;
            lock (_stateLock)
            {
                lastActivity = _lastActivity;
            }
            
            // Check if device is stale (no activity in 5 minutes)
            var isStale = (DateTime.UtcNow - lastActivity).TotalMinutes > DeviceStaleMinutes;
            
            if (isStale && _isConnected)
            {
                _logger.LogWarning("Device appears stale. No activity in {Minutes}+ minutes.", DeviceStaleMinutes);
            }

            var currentMode = "Idle";
            int? activeUserId = null;
            
            // Safely get first pending operation atomically
            var firstOperation = _pendingOperations.Values.FirstOrDefault();
            if (firstOperation != null)
            {
                currentMode = firstOperation.Type == "enroll" ? "Enrollment" : "Verification";
                activeUserId = firstOperation.UserId;
            }

            return await Task.FromResult(new BiometricStatusDto
            {
                IsConnected = IsConnected, // Use the property which includes stale check
                CurrentMode = currentMode,
                ActiveUserId = activeUserId
            });
        }

        // =====================================================
        // EVENT PROCESSING (Called by Hardware Controller)
        // =====================================================

        public async Task ProcessHardwareEventAsync(ESP32HardwareEventDto eventData)
        {
            try
            {
                _logger.LogInformation(
                    "Processing hardware event from {DeviceId}: FingerprintId={FingerprintId}, Status={Status}", 
                    eventData.DeviceId,
                    eventData.FingerprintId, 
                    eventData.Status);

                lock (_stateLock)
                {
                    _lastActivity = DateTime.UtcNow;
                }

                // Update connection status
                if (!_isConnected)
                {
                    _isConnected = true;
                    OnDeviceStatusChanged?.Invoke(this, "connected");
                    _logger.LogInformation("ESP32 device reconnected");
                }

                // Find the pending operation for this slot
                if (!_pendingOperations.TryRemove(eventData.FingerprintId, out var operation))
                {
                    _logger.LogWarning("No pending operation found for slot {SlotId}. Event may be unsolicited.", eventData.FingerprintId);
                    
                    // Still process it, but we don't know the userId
                    // This could happen if backend restarted but ESP32 is still processing
                    var response = new ESP32ResponseDto
                    {
                        Success = eventData.Status == "Success",
                        SlotId = eventData.FingerprintId,
                        UserId = null, // Unknown
                        Message = eventData.Status == "Success" 
                            ? "Operation completed successfully (no pending operation found)" 
                            : (eventData.ErrorMessage ?? "Operation failed")
                    };

                    _logger.LogWarning("Cannot trigger event handlers without userId. Ignoring event.");
                    return;
                }

                // Create response DTO
                var eventResponse = new ESP32ResponseDto
                {
                    Success = eventData.Status == "Success",
                    SlotId = eventData.FingerprintId,
                    UserId = operation.UserId,
                    Message = eventData.Status == "Success" 
                        ? $"{operation.Type} completed successfully" 
                        : (eventData.ErrorMessage ?? $"{operation.Type} failed"),
                    ErrorCode = eventData.Status == "Success" ? null : 1
                };

                // Log operation completion
                var duration = (DateTime.UtcNow - operation.StartedAt).TotalSeconds;
                _logger.LogInformation(
                    "Operation completed: Type={Type}, UserId={UserId}, SlotId={SlotId}, Success={Success}, Duration={Duration}s",
                    operation.Type,
                    operation.UserId,
                    operation.SlotId,
                    eventResponse.Success,
                    duration);

                // Trigger appropriate event based on operation type
                if (operation.Type == "enroll")
                {
                    _logger.LogInformation("Triggering OnEnrollmentCompleted event for UserId={UserId}", operation.UserId);
                    OnEnrollmentCompleted?.Invoke(this, eventResponse);
                }
                else if (operation.Type == "verify")
                {
                    _logger.LogInformation("Triggering OnVerificationCompleted event for UserId={UserId}", operation.UserId);
                    OnVerificationCompleted?.Invoke(this, eventResponse);
                }
                else
                {
                    _logger.LogWarning("Unknown operation type: {Type}", operation.Type);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hardware event");
                throw;
            }
        }

        // =====================================================
        // CONNECTION MANAGEMENT
        // =====================================================

        public async Task<bool> ConnectAsync()
        {
            _isConnected = true;
            lock (_stateLock)
            {
                _lastActivity = DateTime.UtcNow;
            }
            OnDeviceStatusChanged?.Invoke(this, "connected");
            _logger.LogInformation("ESP32 device connected");
            return await Task.FromResult(true);
        }

        public async Task DisconnectAsync()
        {
            _isConnected = false;
            OnDeviceStatusChanged?.Invoke(this, "disconnected");
            _logger.LogInformation("ESP32 device disconnected");
            
            // Clear pending operations
            _pendingOperations.Clear();
            
            await Task.CompletedTask;
        }

        // =====================================================
        // CLEANUP & MAINTENANCE
        // =====================================================

        /// <summary>
        /// Periodically removes stale pending operations to prevent memory leaks
        /// </summary>
        private void CleanupStaleOperations(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var timeoutThreshold = TimeSpan.FromMinutes(OperationTimeoutMinutes);
                var removedCount = 0;

                // Find and remove stale operations
                var staleSlots = _pendingOperations
                    .Where(kvp => (now - kvp.Value.StartedAt) > timeoutThreshold)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var slotId in staleSlots)
                {
                    if (_pendingOperations.TryRemove(slotId, out var operation))
                    {
                        removedCount++;
                        var age = (now - operation.StartedAt).TotalMinutes;
                        
                        _logger.LogWarning(
                            "Removed stale pending operation: Type={Type}, SlotId={SlotId}, UserId={UserId}, Age={Age:F1}min",
                            operation.Type,
                            operation.SlotId,
                            operation.UserId,
                            age
                        );

                        // Trigger failure event for the timed-out operation
                        var timeoutResponse = new ESP32ResponseDto
                        {
                            Success = false,
                            SlotId = operation.SlotId,
                            UserId = operation.UserId,
                            Message = $"Operation timed out after {OperationTimeoutMinutes} minutes",
                            ErrorCode = -1 // Timeout error code
                        };

                        // Notify listeners about the timeout
                        if (operation.Type == "enroll")
                        {
                            OnEnrollmentCompleted?.Invoke(this, timeoutResponse);
                        }
                        else if (operation.Type == "verify")
                        {
                            OnVerificationCompleted?.Invoke(this, timeoutResponse);
                        }
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleanup completed: Removed {Count} stale operation(s)", removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup of stale operations");
            }
        }

        // =====================================================
        // HELPER CLASSES
        // =====================================================

        private class PendingOperation
        {
            public string Type { get; set; } = string.Empty; // "enroll" or "verify"
            public int SlotId { get; set; }
            public int UserId { get; set; }
            public DateTime StartedAt { get; set; }
        }

        // =====================================================
        // DISPOSE PATTERN
        // =====================================================

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _cleanupTimer?.Dispose();
                    _pendingOperations.Clear();
                    _logger.LogInformation("RealESP32Service disposed");
                }
                _disposed = true;
            }
        }
    }
}
