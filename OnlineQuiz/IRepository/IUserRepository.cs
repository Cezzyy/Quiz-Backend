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
    }
}
