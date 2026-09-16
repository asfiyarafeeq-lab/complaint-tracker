using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Repositories
{
    public interface IUserRepository
    {
        /// <summary>The account with this username, or null. Used by login.</summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>The account with this id, or null.</summary>
        Task<User?> GetByIdAsync(int id);

        Task<bool> UsernameExistsAsync(string username);

        Task<int> CreateAsync(User user);
    }
}
