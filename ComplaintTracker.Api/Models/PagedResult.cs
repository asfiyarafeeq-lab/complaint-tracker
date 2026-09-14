namespace ComplaintTracker.Api.Models
{
    /// <summary>
    /// One page of results plus the paging information needed to request the rest.
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

        /// <summary>The page returned, counting from 1.</summary>
        public int Page { get; set; }

        public int PageSize { get; set; }

        /// <summary>Rows matching the filters, ignoring paging.</summary>
        public int TotalCount { get; set; }

        public int TotalPages =>
            PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;
    }
}
