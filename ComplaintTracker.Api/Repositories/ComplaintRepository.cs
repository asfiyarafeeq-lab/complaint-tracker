using System.Data;
using ComplaintTracker.Api.Models;
using Dapper;

namespace ComplaintTracker.Api.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly IDbConnection _connection;

        public ComplaintRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<Complaint>> GetAllAsync()
        {
            const string sql = @"
                SELECT Id, Title, Description, Category, Status, CreatedDate, RaisedBy
                FROM dbo.Complaints
                ORDER BY Id;";

            return await _connection.QueryAsync<Complaint>(sql);
        }

        public async Task<Complaint?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Title, Description, Category, Status, CreatedDate, RaisedBy
                FROM dbo.Complaints
                WHERE Id = @Id;";

            return await _connection.QuerySingleOrDefaultAsync<Complaint>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Complaint complaint)
        {
            // Id is an IDENTITY column, so it is left out of the insert and
            // read back from the same statement.
            const string sql = @"
                INSERT INTO dbo.Complaints (Title, Description, Category, Status, CreatedDate, RaisedBy)
                VALUES (@Title, @Description, @Category, @Status, @CreatedDate, @RaisedBy);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await _connection.QuerySingleAsync<int>(sql, complaint);
        }

        public async Task<bool> UpdateAsync(Complaint complaint)
        {
            // CreatedDate is deliberately not updated.
            const string sql = @"
                UPDATE dbo.Complaints
                SET Title = @Title,
                    Description = @Description,
                    Category = @Category,
                    Status = @Status,
                    RaisedBy = @RaisedBy
                WHERE Id = @Id;";

            var rows = await _connection.ExecuteAsync(sql, complaint);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM dbo.Complaints WHERE Id = @Id;";

            var rows = await _connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }
    }
}
