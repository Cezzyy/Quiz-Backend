using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;

namespace OnlineQuiz.Services
{
    public class ExportImportLogService : IExportImportLogService
    {
        private readonly IExportImportLogRepository _logRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public ExportImportLogService(
            IExportImportLogRepository logRepository,
            IUserRoleRepository userRoleRepository)
        {
            _logRepository = logRepository;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<ExportImportLogResponseDto> CreateLogAsync(CreateExportImportLogDto createLogDto)
        {
            var log = createLogDto.Adapt<ExportImportLog>();
            log.Status = "Pending";
            log.CreatedAt = DateTime.UtcNow;

            var createdLog = await _logRepository.CreateAsync(log);
            return createdLog.Adapt<ExportImportLogResponseDto>();
        }

        public async Task<ExportImportLogResponseDto?> GetLogByIdAsync(int logId, int userId)
        {
            var log = await _logRepository.GetByIdAsync(logId);
            if (log == null)
            {
                return null;
            }

            // Users can only view their own logs unless they're admin
            var isAdmin = await _userRoleRepository.IsAdminAsync(userId);
            if (log.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You can only view your own export/import logs");
            }

            return log.Adapt<ExportImportLogResponseDto>();
        }

        public async Task<List<ExportImportLogResponseDto>> GetLogsForUserAsync(int userId)
        {
            var logs = await _logRepository.GetByUserIdAsync(userId);
            return logs.Adapt<List<ExportImportLogResponseDto>>();
        }

        public async Task<ExportImportLogResponseDto> UpdateLogStatusAsync(int logId, UpdateExportImportLogDto updateLogDto, int userId)
        {
            var log = await _logRepository.GetByIdAsync(logId);
            if (log == null)
            {
                throw new ArgumentException($"Export/Import log with ID {logId} not found");
            }

            // Users can only update their own logs unless they're admin
            var isAdmin = await _userRoleRepository.IsAdminAsync(userId);
            if (log.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You can only update your own export/import logs");
            }

            log.Status = updateLogDto.Status;
            log.CompletedAt = updateLogDto.CompletedAt;
            log.ErrorMessage = updateLogDto.ErrorMessage;

            var updatedLog = await _logRepository.UpdateAsync(log);
            return updatedLog.Adapt<ExportImportLogResponseDto>();
        }

        public async Task<bool> DeleteLogAsync(int logId, int userId)
        {
            var log = await _logRepository.GetByIdAsync(logId);
            if (log == null)
            {
                return false;
            }

            // Users can only delete their own logs unless they're admin
            var isAdmin = await _userRoleRepository.IsAdminAsync(userId);
            if (log.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You can only delete your own export/import logs");
            }

            return await _logRepository.DeleteAsync(logId);
        }
    }
}
