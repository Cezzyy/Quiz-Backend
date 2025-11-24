using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IUserRoleRepository
    {
        Task<UserRole> CreateAsync(UserRole userRole);
        Task<List<UserRole>> GetByUserIdAsync(int userId);
        Task<List<UserRole>> GetAllAsync();
        Task<bool> DeleteByUserIdAsync(int userId);
    }
}
