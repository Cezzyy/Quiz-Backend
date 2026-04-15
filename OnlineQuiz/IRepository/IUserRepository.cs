using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user);
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetAllAsync();
        Task<List<User>> GetByIdsAsync(List<int> userIds);
        Task<int> CountAsync();
        Task<int> CountByRoleAsync(int roleId);
        Task<List<User>> GetRecentRegistrationsAsync(int days);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int userId);
        Task<int> BulkDeleteAsync(List<int> userIds);

        // Archive operations
        Task<User?> ArchiveAsync(int userId, int archivedBy);
        Task<User?> UnarchiveAsync(int userId);
        Task<int> BulkArchiveAsync(List<int> userIds, int archivedBy);
        Task<int> BulkUnarchiveAsync(List<int> userIds);
        Task<List<User>> GetArchivedAsync();
        Task<List<User>> GetAllIncludingArchivedAsync();
        Task<int> CountArchivedAsync();
    }
}
