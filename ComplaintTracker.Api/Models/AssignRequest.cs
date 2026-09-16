namespace ComplaintTracker.Api.Models
{
    /// <summary>
    /// Who should work a ticket. Null hands it back to the unassigned pile.
    /// </summary>
    public class AssignRequest
    {
        public int? AssignedToUserId { get; set; }
    }
}
