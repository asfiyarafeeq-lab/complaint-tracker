using System.Data;
using ComplaintTracker.Api.Models;
using Dapper;

namespace ComplaintTracker.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            const string sql = @"
                SELECT Id, Username, PasswordHash, Role, CreatedDate
                FROM dbo.Users
                WHERE Username = @Username;";

            return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Username = @Username;";

            return await _connection.ExecuteScalarAsync<int>(sql, new { Username = username }) > 0;
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO dbo.Users (Username, PasswordHash, Role, CreatedDate)
                VALUES (@Username, @PasswordHash, @Role, @CreatedDate);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await _connection.QuerySingleAsync<int>(sql, user);
        }
    }
}
