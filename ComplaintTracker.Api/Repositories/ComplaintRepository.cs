using System.Data;
using ComplaintTracker.Api.Models;
using Dapper;

namespace ComplaintTracker.Api.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private const string Columns =
            "Id, Title, Description, Category, Status, CreatedDate, RaisedBy, RaisedByUserId, " +
            "AssignedTo, AssignedToUserId";

        private readonly IDbConnection _connection;

        public ComplaintRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Wraps a keyword as a LIKE pattern, escaping the characters SQL treats
        /// as wildcards so a title containing "50%" is matched literally.
        /// </summary>
        private static string? ToContainsPattern(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return null;
            }

            var escaped = keyword.Trim()
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_")
                .Replace("[", @"\[");

            return $"%{escaped}%";
        }

        public async Task<PagedResult<Complaint>> SearchAsync(
            string? status,
            string? category,
            string? search,
            int? raisedByUserId,
            int? assignedToUserId,
            ComplaintSortField sortField,
            SortDirection sortDirection,
            int page,
            int pageSize)
        {
            // ORDER BY cannot be parameterised, so the clause is chosen from a
            // fixed set rather than built from caller input. Id breaks ties so
            // the order stays stable when rows share a value.
            var descending = sortDirection == SortDirection.Descending;
            var orderBy = sortField switch
            {
                ComplaintSortField.Title => descending
                    ? "Title DESC, Id DESC"
                    : "Title ASC, Id ASC",
                _ => descending
                    ? "CreatedDate DESC, Id DESC"
                    : "CreatedDate ASC, Id ASC"
            };

            // Each filter drops out of the WHERE clause when it is null, so one
            // parameterised statement covers every combination. The count and the
            // page share those filters and travel as a single round trip.
            const string where = @"
                WHERE (@Status IS NULL OR Status = @Status)
                  AND (@Category IS NULL OR Category = @Category)
                  AND (@Search IS NULL OR Title LIKE @Search ESCAPE '\')
                  AND (@RaisedByUserId IS NULL OR RaisedByUserId = @RaisedByUserId)
                  AND (@AssignedToUserId IS NULL OR AssignedToUserId = @AssignedToUserId)";

            var sql = $@"
                SELECT COUNT(*)
                FROM dbo.Complaints
                {where};

                SELECT {Columns}
                FROM dbo.Complaints
                {where}
                ORDER BY {orderBy}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new
            {
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
                Search = ToContainsPattern(search),
                RaisedByUserId = raisedByUserId,
                AssignedToUserId = assignedToUserId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            };

            using var results = await _connection.QueryMultipleAsync(sql, parameters);

            var totalCount = await results.ReadSingleAsync<int>();
            var items = await results.ReadAsync<Complaint>();

            return new PagedResult<Complaint>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Complaint?> GetByIdAsync(int id)
        {
            var sql = $@"
                SELECT {Columns}
                FROM dbo.Complaints
                WHERE Id = @Id;";

            return await _connection.QuerySingleOrDefaultAsync<Complaint>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Complaint complaint)
        {
            // Id is an IDENTITY column, so it is left out of the insert and
            // read back from the same statement.
            const string sql = @"
                INSERT INTO dbo.Complaints
                    (Title, Description, Category, Status, CreatedDate, RaisedBy, RaisedByUserId)
                VALUES
                    (@Title, @Description, @Category, @Status, @CreatedDate, @RaisedBy, @RaisedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await _connection.QuerySingleAsync<int>(sql, complaint);
        }

        public async Task<bool> UpdateAsync(Complaint complaint)
        {
            // CreatedDate and the owner are deliberately not updated.
            const string sql = @"
                UPDATE dbo.Complaints
                SET Title = @Title,
                    Description = @Description,
                    Category = @Category,
                    Status = @Status
                WHERE Id = @Id;";

            var rows = await _connection.ExecuteAsync(sql, complaint);
            return rows > 0;
        }

        public async Task<bool> AssignAsync(int complaintId, int? assignedToUserId, string? assignedTo)
        {
            const string sql = @"
                UPDATE dbo.Complaints
                SET AssignedToUserId = @AssignedToUserId,
                    AssignedTo = @AssignedTo
                WHERE Id = @Id;";

            var rows = await _connection.ExecuteAsync(
                sql,
                new { Id = complaintId, AssignedToUserId = assignedToUserId, AssignedTo = assignedTo });

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
