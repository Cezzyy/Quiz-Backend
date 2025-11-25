using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IAuthRepository
    {
        /// <summary>
        /// Verify user credentials using Supabase RPC function
        /// </summary>
        Task<User?> VerifyUserCredentialsAsync(string email, string password);

        /// <summary>
        /// Get user with roles and additional data (Student/Teacher)
        /// </summary>
        Task<(User user, UserRole userRole, Student? student, Teacher? teacher)?> GetUserWithRolesAsync(int userId);

        /// <summary>
        /// Update user password
        /// </summary>
        Task UpdatePasswordAsync(int userId, string newPasswordHash);
    }
}
