namespace ComplaintTracker.Api.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string RaisedBy { get; set; } = string.Empty;
    }
}
