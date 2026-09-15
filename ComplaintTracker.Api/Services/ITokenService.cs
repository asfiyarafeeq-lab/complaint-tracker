using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Services
{
    public interface ITokenService
    {
        /// <summary>
        /// Builds a signed token carrying the user's id, username and role,
        /// along with the moment it stops being valid.
        /// </summary>
        (string Token, DateTime ExpiresAt) CreateToken(User user);
    }
}
