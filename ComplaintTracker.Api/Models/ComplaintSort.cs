namespace ComplaintTracker.Api.Models
{
    /// <summary>The column a complaint listing is ordered by.</summary>
    public enum ComplaintSortField
    {
        CreatedDate,
        Title
    }

    /// <summary>The direction a complaint listing is ordered in.</summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }
}
