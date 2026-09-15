namespace ComplaintTracker.Api.Models
{
    /// <summary>
    /// The roles an account may hold. Kept in step with the CK_Users_Role
    /// constraint in Database/schema.sql; change both together.
    /// </summary>
    public static class UserRoles
    {
        /// <summary>Raises tickets and sees their own.</summary>
        public const string User = "User";

        /// <summary>IT staff: works the tickets assigned to them.</summary>
        public const string Agent = "Agent";

        /// <summary>Assigns tickets, manages accounts, sees everything.</summary>
        public const string Admin = "Admin";

        public static readonly string[] All = { User, Agent, Admin };
    }
}
