using System.ComponentModel.DataAnnotations;

namespace ComplaintTracker.Api.Models
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        // Long enough to be worth hashing; the upper bound only stops someone
        // posting a megabyte of text at the hasher.
        [Required]
        [MinLength(8)]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>What a successful login hands back. No password, no hash.</summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
