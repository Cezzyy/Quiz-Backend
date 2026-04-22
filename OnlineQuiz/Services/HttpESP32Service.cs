using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Services
{
    public class PendingCommand
    {
        public string CommandType { get; set; } = string.Empty; // "ENROLL" or "VERIFY"
        public int SlotId { get; set; }
        public int UserId { get; set; }
        public DateTime IssuedAt { get; set; }
    }

    public class HttpESP32Service : IESP32Service
    {
        private readonly ILogger<HttpESP32Service> _logger;
        private readonly object _syncLock = new object();
        private bool _isConnected;
        private string _currentMode = "Idle"; // Idle, Enrollment, Verification
        private int? _activeUserId;
        private DateTime _lastHeartbeat = DateTime.MinValue;
        
        private PendingCommand? _pendingCommand = null;

        public event EventHandler<ESP32ResponseDto>? OnEnrollmentCompleted;
        public event EventHandler<ESP32ResponseDto>? OnVerificationCompleted;
        public event EventHandler<string>? OnDeviceStatusChanged;

        public HttpESP32Service(ILogger<HttpESP32Service> logger)
        {
            _logger = logger;
            _isConnected = false; // Initially false, will be set to true when ESP32 polls
        }

        public bool IsConnected 
        {
            get 
            {
                lock (_syncLock)
                {
                    // Give it a 30 second timeout on heartbeats
                    return _isConnected && (DateTime.UtcNow - _lastHeartbeat).TotalSeconds < 30;
                }
            }
        }

        // Called by backend BiometricService
        public Task<ESP32ResponseDto> SendEnrollCommandAsync(int slotId, int userId)
        {
            _logger.LogInformation("Queueing Enroll command for slot {SlotId}, user {UserId}", slotId, userId);
            
            lock (_syncLock)
            {
                _pendingCommand = new PendingCommand 
                { 
                    CommandType = "ENROLL", 
                    SlotId = slotId, 
                    UserId = userId,
                    IssuedAt = DateTime.UtcNow
                };
                
                _currentMode = "Enrollment";
                _activeUserId = userId;
            }
            OnDeviceStatusChanged?.Invoke(this, "Enrollment");

            return Task.FromResult(new ESP32ResponseDto
            {
                Success = true,
                Message = "Enrollment command queued for ESP32 polling",
                UserId = userId,
                SlotId = slotId
            });
        }

        // Called by backend BiometricService
        public Task<ESP32ResponseDto> SendVerifyCommandAsync(int slotId, int userId)
        {
            _logger.LogInformation("Queueing Verify command for slot {SlotId}, user {UserId}", slotId, userId);
            
            lock (_syncLock)
            {
                _pendingCommand = new PendingCommand 
                { 
                    CommandType = "VERIFY", 
                    SlotId = slotId, 
                    UserId = userId,
                    IssuedAt = DateTime.UtcNow
                };

                _currentMode = "Verification";
                _activeUserId = userId;
            }
            OnDeviceStatusChanged?.Invoke(this, "Verification");

            return Task.FromResult(new ESP32ResponseDto
            {
                Success = true,
                Message = "Verification command queued for ESP32 polling",
                UserId = userId,
                SlotId = slotId
            });
        }

        // Called by backend BiometricService
        public Task<ESP32ResponseDto> CancelOperationAsync()
        {
            _logger.LogInformation("Cancelling current operation via HttpESP32Service");
            
            lock (_syncLock)
            {
                _pendingCommand = null;
                _currentMode = "Idle";
                _activeUserId = null;
            }
            OnDeviceStatusChanged?.Invoke(this, "Idle");

            return Task.FromResult(new ESP32ResponseDto
            {
                Success = true,
                Message = "Operation cancelled"
            });
        }

        // Called by backend BiometricService
        public Task<BiometricStatusDto> GetDeviceStatusAsync()
        {
            bool wasConnected;
            bool currentConnected;
            string currentMode;
            int? activeUserId;

            lock (_syncLock)
            {
                wasConnected = _isConnected;
                // Evaluate IsConnected property logic here manually to avoid deadlock if IsConnected also locks, 
                // but IsConnected uses the same lock so it's fine if locks are reentrant. 
                // However, C# lock is reentrant. So we can just use IsConnected.
                currentConnected = _isConnected && (DateTime.UtcNow - _lastHeartbeat).TotalSeconds < 30;
                
                if (!currentConnected && wasConnected)
                {
                    _isConnected = false;
                }

                currentMode = _currentMode;
                activeUserId = _activeUserId;
            }

            if (!currentConnected && wasConnected)
            {
                OnDeviceStatusChanged?.Invoke(this, "Disconnected");
            }

            return Task.FromResult(new BiometricStatusDto
            {
                IsConnected = currentConnected,
                CurrentMode = currentMode,
                ActiveUserId = activeUserId
            });
        }

        // Keep interface compatibility
        public Task<bool> ConnectAsync()
        {
            lock (_syncLock)
            {
                _isConnected = true;
                _lastHeartbeat = DateTime.UtcNow;
            }
            OnDeviceStatusChanged?.Invoke(this, "Connected");
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            lock (_syncLock)
            {
                _isConnected = false;
            }
            OnDeviceStatusChanged?.Invoke(this, "Disconnected");
            return Task.CompletedTask;
        }

        // Keep interface compatibility for hardware event routing
        public Task ProcessHardwareEventAsync(ESP32HardwareEventDto eventData)
        {
            // The new HTTP polling service processes events differently via SubmitResult
            // so we don't need to do anything here for now.
            return Task.CompletedTask;
        }

        // --- HTTP Polling Specific Methods ---

        // Called by ESP32Controller GET /api/esp32/poll
        public PendingCommand? PopCommand()
        {
            Heartbeat(); // Update connection status since device is polling

            lock (_syncLock)
            {
                var cmd = _pendingCommand;
                _pendingCommand = null; // Clear command so it only executes once
                return cmd;
            }
        }

        // Called by ESP32Controller POST /api/esp32/result
        public void SubmitResult(ESP32ResponseDto result, string commandType)
        {
            Heartbeat();
            _logger.LogInformation("Received {CommandType} result from ESP32: Success={Success}", commandType, result.Success);
            
            lock (_syncLock)
            {
                _currentMode = "Idle";
                _activeUserId = null;
            }
            OnDeviceStatusChanged?.Invoke(this, "Idle");

            if (commandType == "ENROLL")
            {
                OnEnrollmentCompleted?.Invoke(this, result);
            }
            else if (commandType == "VERIFY")
            {
                OnVerificationCompleted?.Invoke(this, result);
            }
        }
        
        // Let ESP32 update its presence
        public void Heartbeat()
        {
            bool wasDisconnected = false;
            lock (_syncLock)
            {
                _lastHeartbeat = DateTime.UtcNow;
                if (!_isConnected)
                {
                    _isConnected = true;
                    wasDisconnected = true;
                }
            }
            
            if (wasDisconnected)
            {
                OnDeviceStatusChanged?.Invoke(this, "Connected");
            }
        }
    }
}
