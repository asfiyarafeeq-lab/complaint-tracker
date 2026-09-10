using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Repositories
{
    public interface IComplaintRepository
    {
        Task<IEnumerable<Complaint>> GetAllAsync();

        Task<Complaint?> GetByIdAsync(int id);

        Task<int> CreateAsync(Complaint complaint);

        Task<bool> UpdateAsync(Complaint complaint);

        Task<bool> DeleteAsync(int id);
    }
}
