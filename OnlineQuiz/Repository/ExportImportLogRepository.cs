using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class ExportImportLogRepository : IExportImportLogRepository
    {
        private readonly SupabaseService _supabaseService;

        public ExportImportLogRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<ExportImportLog> CreateAsync(ExportImportLog log)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ExportImportLog>().Insert(log);
            return result.Models.First();
        }

        public async Task<ExportImportLog?> GetByIdAsync(int logId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ExportImportLog>()
                .Where(l => l.LogId == logId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<ExportImportLog>> GetByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ExportImportLog>()
                .Where(l => l.UserId == userId)
                .Order("CreatedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<ExportImportLog> UpdateAsync(ExportImportLog log)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ExportImportLog>().Update(log);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int logId)
        {
            var client = _supabaseService.GetClient();
            await client.From<ExportImportLog>()
                .Where(l => l.LogId == logId)
                .Delete();
            return true;
        }
    }
}
