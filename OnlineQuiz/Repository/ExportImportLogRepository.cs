using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using Postgrest;

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
            var options = new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation };
            var result = await client.From<ExportImportLog>().Insert(log, options);
            var created = result.Models.First();
            
            // If LogId is not populated, fetch by UserId and FileName
            if (created.LogId == 0)
            {
                var fetchResult = await client.From<ExportImportLog>()
                    .Where(l => l.UserId == log.UserId && l.FileName == log.FileName)
                    .Order("CreatedAt", Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();
                return fetchResult.Models.FirstOrDefault() ?? created;
            }
            
            return created;
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
