namespace ComplaintTracker.Api.Models
{
    /// <summary>
    /// The categories a ticket may be filed under. Kept in step with the
    /// CK_Complaints_Category constraint in Database/schema.sql; change both
    /// together, and check no existing row uses a value being removed.
    /// </summary>
    public static class ComplaintCategories
    {
        public const string Hardware = "Hardware";
        public const string Software = "Software";
        public const string Network = "Network";
        public const string AccountAccess = "Account Access";
        public const string Email = "Email";
        public const string Printer = "Printer";
        public const string Other = "Other";

        public static readonly string[] All =
        {
            Hardware, Software, Network, AccountAccess, Email, Printer, Other
        };

        /// <summary>
        /// True when the value matches one of the categories exactly. Whitespace
        /// around it is ignored, case is not, matching how statuses behave.
        /// </summary>
        public static bool IsValid(string value) =>
            All.Contains(value.Trim(), StringComparer.Ordinal);
    }
}
