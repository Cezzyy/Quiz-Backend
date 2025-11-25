using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IExportImportLogRepository
    {
        Task<ExportImportLog> CreateAsync(ExportImportLog log);
        Task<ExportImportLog?> GetByIdAsync(int logId);
        Task<List<ExportImportLog>> GetByUserIdAsync(int userId);
        Task<ExportImportLog> UpdateAsync(ExportImportLog log);
        Task<bool> DeleteAsync(int logId);
    }
}
