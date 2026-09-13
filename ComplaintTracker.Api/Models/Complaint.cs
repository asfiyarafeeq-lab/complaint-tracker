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
        public string Status { get; set; } = string.Empty;

        // Set by the controller on create; not supplied by the caller.
        public DateTime CreatedDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string RaisedBy { get; set; } = string.Empty;
    }
}
