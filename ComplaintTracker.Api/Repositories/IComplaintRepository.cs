using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Repositories
{
    public interface IComplaintRepository
    {
        /// <summary>
        /// Returns one page of complaints ordered by the given field and
        /// direction, optionally narrowed by status, category, and a keyword
        /// matched anywhere in the title. A null or blank filter is ignored, so
        /// passing none considers all rows. TotalCount on the result reflects
        /// the filters but not the paging.
        /// </summary>
        Task<PagedResult<Complaint>> SearchAsync(
            string? status,
            string? category,
            string? search,
            ComplaintSortField sortField,
            SortDirection sortDirection,
            int page,
            int pageSize);

        Task<Complaint?> GetByIdAsync(int id);

        Task<int> CreateAsync(Complaint complaint);

        Task<bool> UpdateAsync(Complaint complaint);

        Task<bool> DeleteAsync(int id);
    }
}
