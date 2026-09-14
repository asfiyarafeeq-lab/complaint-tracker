using ComplaintTracker.Api.Models;
using ComplaintTracker.Api.Repositories;

namespace ComplaintTracker.Api.Tests
{
    /// <summary>
    /// Stands in for the real repository so controller rules can be checked
    /// without a database. Records what it was asked for and returns whatever
    /// the test sets up.
    /// </summary>
    public class FakeComplaintRepository : IComplaintRepository
    {
        // What the controller passed down on the last SearchAsync call.
        public string? LastStatus { get; private set; }
        public string? LastCategory { get; private set; }
        public string? LastSearch { get; private set; }
        public ComplaintSortField LastSortField { get; private set; }
        public SortDirection LastSortDirection { get; private set; }
        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }
        public bool SearchWasCalled { get; private set; }

        // What the fake should hand back.
        public Complaint? ComplaintToReturn { get; set; }
        public int NewId { get; set; } = 42;
        public bool UpdateSucceeds { get; set; } = true;
        public bool DeleteSucceeds { get; set; } = true;

        // What the controller handed over on the last write.
        public Complaint? CreatedComplaint { get; private set; }
        public Complaint? UpdatedComplaint { get; private set; }
        public int? DeletedId { get; private set; }

        public Task<PagedResult<Complaint>> SearchAsync(
            string? status,
            string? category,
            string? search,
            ComplaintSortField sortField,
            SortDirection sortDirection,
            int page,
            int pageSize)
        {
            SearchWasCalled = true;
            LastStatus = status;
            LastCategory = category;
            LastSearch = search;
            LastSortField = sortField;
            LastSortDirection = sortDirection;
            LastPage = page;
            LastPageSize = pageSize;

            return Task.FromResult(new PagedResult<Complaint>
            {
                Items = Array.Empty<Complaint>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            });
        }

        public Task<Complaint?> GetByIdAsync(int id) => Task.FromResult(ComplaintToReturn);

        public Task<int> CreateAsync(Complaint complaint)
        {
            CreatedComplaint = complaint;
            return Task.FromResult(NewId);
        }

        public Task<bool> UpdateAsync(Complaint complaint)
        {
            UpdatedComplaint = complaint;
            return Task.FromResult(UpdateSucceeds);
        }

        public Task<bool> DeleteAsync(int id)
        {
            DeletedId = id;
            return Task.FromResult(DeleteSucceeds);
        }
    }
}
