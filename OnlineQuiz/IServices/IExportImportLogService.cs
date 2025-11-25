using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IExportImportLogService
    {
        Task<ExportImportLogResponseDto> CreateLogAsync(CreateExportImportLogDto createLogDto);
        Task<ExportImportLogResponseDto?> GetLogByIdAsync(int logId, int userId);
        Task<List<ExportImportLogResponseDto>> GetLogsForUserAsync(int userId);
        Task<ExportImportLogResponseDto> UpdateLogStatusAsync(int logId, UpdateExportImportLogDto updateLogDto, int userId);
        Task<bool> DeleteLogAsync(int logId, int userId);
    }
}
