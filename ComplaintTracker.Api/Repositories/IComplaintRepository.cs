using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Repositories
{
    public interface IComplaintRepository
    {
        /// <summary>
        /// Returns one page of complaints ordered by CreatedDate, optionally
        /// narrowed by status and/or category. A null or blank filter is ignored,
        /// so passing neither considers all rows. TotalCount on the result
        /// reflects the filters but not the paging.
        /// </summary>
        Task<PagedResult<Complaint>> SearchAsync(
            string? status, string? category, bool newestFirst, int page, int pageSize);

        Task<Complaint?> GetByIdAsync(int id);

        Task<int> CreateAsync(Complaint complaint);

        Task<bool> UpdateAsync(Complaint complaint);

        Task<bool> DeleteAsync(int id);
    }
}
