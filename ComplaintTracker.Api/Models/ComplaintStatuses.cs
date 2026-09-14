namespace ComplaintTracker.Api.Models
{
    /// <summary>
    /// The statuses a complaint may hold. Kept in step with the
    /// CK_Complaints_Status constraint in Database/schema.sql; change both
    /// together.
    /// </summary>
    public static class ComplaintStatuses
    {
        public const string Open = "Open";
        public const string InProgress = "In Progress";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";

        public static readonly string[] All = { Open, InProgress, Resolved, Closed };

        /// <summary>
        /// True when the value matches one of the statuses exactly. Surrounding
        /// whitespace is ignored, case is not: "open" is not "Open". This keeps
        /// the query filter as strict as the AllowedValues check on writes.
        /// </summary>
        public static bool IsValid(string value) =>
            All.Contains(value.Trim(), StringComparer.Ordinal);
    }
}
