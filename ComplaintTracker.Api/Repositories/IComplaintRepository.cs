using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Repositories
{
    public interface IComplaintRepository
    {
        /// <summary>
        /// Returns complaints, optionally narrowed by status and/or category.
        /// A null or blank filter is ignored, so passing neither returns all rows.
        /// </summary>
        Task<IEnumerable<Complaint>> SearchAsync(string? status, string? category);

        Task<Complaint?> GetByIdAsync(int id);

        Task<int> CreateAsync(Complaint complaint);

        Task<bool> UpdateAsync(Complaint complaint);

        Task<bool> DeleteAsync(int id);
    }
}
