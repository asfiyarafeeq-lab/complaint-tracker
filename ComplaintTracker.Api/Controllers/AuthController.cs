using ComplaintTracker.Api.Models;
using ComplaintTracker.Api.Repositories;
using ComplaintTracker.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly ITokenService _tokens;
        private readonly IPasswordHasher<User> _hasher;

        public AuthController(
            IUserRepository users,
            ITokenService tokens,
            IPasswordHasher<User> hasher)
        {
            _users = users;
            _tokens = tokens;
            _hasher = hasher;
        }

        /// <summary>
        /// Creates an account. New accounts are always plain Users; promote to
        /// Agent or Admin deliberately, so nobody can sign up as an admin.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var username = request.Username.Trim();

            if (await _users.UsernameExistsAsync(username))
            {
                ModelState.AddModelError(nameof(request.Username), "That username is already taken.");
                return ValidationProblem(ModelState);
            }

            var user = new User
            {
                Username = username,
                Role = UserRoles.User,
                CreatedDate = DateTime.UtcNow
            };

            // The hash depends on the user object, so it is set after the rest.
            user.PasswordHash = _hasher.HashPassword(user, request.Password);
            user.Id = await _users.CreateAsync(user);

            return Ok(new { user.Id, user.Username, user.Role });
        }

        /// <summary>Exchanges a username and password for a token.</summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var user = await _users.GetByUsernameAsync(request.Username.Trim());

            // A missing account and a wrong password give the same answer, so
            // the response cannot be used to discover which usernames exist.
            if (user is null)
            {
                return Unauthorized(new { message = "Username or password is incorrect." });
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Username or password is incorrect." });
            }

            var (token, expiresAt) = _tokens.CreateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expiresAt
            });
        }
    }
}
