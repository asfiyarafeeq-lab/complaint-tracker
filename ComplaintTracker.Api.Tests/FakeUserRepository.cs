using ComplaintTracker.Api.Models;
using ComplaintTracker.Api.Repositories;

namespace ComplaintTracker.Api.Tests
{
    /// <summary>
    /// Stands in for the accounts table. Tests add whichever accounts they need
    /// with <see cref="Add"/>.
    /// </summary>
    public class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public User Add(int id, string username, string role)
        {
            var user = new User { Id = id, Username = username, Role = role };
            _users.Add(user);
            return user;
        }

        public Task<User?> GetByUsernameAsync(string username) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Username == username));

        public Task<User?> GetByIdAsync(int id) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<bool> UsernameExistsAsync(string username) =>
            Task.FromResult(_users.Any(u => u.Username == username));

        public Task<int> CreateAsync(User user)
        {
            user.Id = _users.Count + 1;
            _users.Add(user);
            return Task.FromResult(user.Id);
        }
    }
}
