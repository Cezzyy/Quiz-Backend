using OnlineQuiz.IRepository;
using OnlineQuiz.Models;

namespace OnlineQuiz.Tests.Fakes
{
    /// <summary>
    /// Shared in-memory IUserRepository used across service tests.
    /// Replaces 5 near-identical per-file copies.
    /// Seed users with <see cref="Seed"/> before running tests.
    /// </summary>
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _store = new();

        public void Seed(User u) => _store[u.UserId] = u;

        public Task<User?> GetByIdAsync(int userId)
        {
            _store.TryGetValue(userId, out var u);
            return Task.FromResult(u);
        }

        public Task<User?> GetByEmailAsync(string email)
            => Task.FromResult(_store.Values.FirstOrDefault(u => u.Email == email));

        public Task<List<User>> GetAllAsync()
            => Task.FromResult(_store.Values.ToList());

        public Task<List<User>> GetByIdsAsync(List<int> userIds)
            => Task.FromResult(_store.Values.Where(u => userIds.Contains(u.UserId)).ToList());

        public Task<User> CreateAsync(User user)
        {
            _store[user.UserId] = user;
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user)
        {
            _store[user.UserId] = user;
            return Task.FromResult(user);
        }

        public Task<bool> DeleteAsync(int userId)
            => Task.FromResult(_store.Remove(userId));

        public Task<int> BulkDeleteAsync(List<int> userIds)
        {
            var count = 0;
            foreach (var id in userIds)
                if (_store.Remove(id)) count++;
            return Task.FromResult(count);
        }

        public Task<int> CountAsync() => Task.FromResult(_store.Count);
        public Task<int> CountByRoleAsync(int roleId) => Task.FromResult(0);
        public Task<List<User>> GetRecentRegistrationsAsync(int days) => Task.FromResult(new List<User>());

        // Archive stubs
        public Task<User?> ArchiveAsync(int userId, int archivedBy) => Task.FromResult<User?>(null);
        public Task<User?> UnarchiveAsync(int userId) => Task.FromResult<User?>(null);
        public Task<int> BulkArchiveAsync(List<int> userIds, int archivedBy) => Task.FromResult(0);
        public Task<int> BulkUnarchiveAsync(List<int> userIds) => Task.FromResult(0);
        public Task<List<User>> GetArchivedAsync() => Task.FromResult(new List<User>());
        public Task<List<User>> GetAllIncludingArchivedAsync() => Task.FromResult(_store.Values.ToList());
        public Task<int> CountArchivedAsync() => Task.FromResult(0);
    }
}
