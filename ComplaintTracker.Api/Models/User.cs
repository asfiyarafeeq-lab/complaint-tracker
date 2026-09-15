namespace ComplaintTracker.Api.Models
{
    /// <summary>
    /// An account. PasswordHash never leaves the API: it is written by the
    /// register endpoint and only ever compared against, never returned.
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = UserRoles.User;

        public DateTime CreatedDate { get; set; }
    }
}
