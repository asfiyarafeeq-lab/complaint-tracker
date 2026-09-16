using System.ComponentModel.DataAnnotations;

namespace ComplaintTracker.Api.Models
{
    public class Complaint
    {
        // Assigned by SQL Server; ignored on create and update.
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Maps to nvarchar(max), so no length limit.
        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [AllowedValues(
            ComplaintStatuses.Open,
            ComplaintStatuses.InProgress,
            ComplaintStatuses.Resolved,
            ComplaintStatuses.Closed)]
        public string Status { get; set; } = string.Empty;

        // Set by the controller on create; not supplied by the caller.
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Display name of whoever raised this. Taken from the caller's token,
        /// so anything sent in the request body is ignored.
        /// </summary>
        public string RaisedBy { get; set; } = string.Empty;

        /// <summary>
        /// The account that raised this, used to decide who may see it. Null on
        /// rows created before accounts existed.
        /// </summary>
        public int? RaisedByUserId { get; set; }
    }
}
