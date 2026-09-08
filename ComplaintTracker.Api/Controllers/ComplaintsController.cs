using ComplaintTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComplaintsController : ControllerBase
    {
        // Temporary store until a database is wired up. Static so the data
        // survives between requests; the controller itself is per-request.
        private static readonly List<Complaint> Complaints = new();
        private static int _nextId = 1;

        [HttpGet]
        public ActionResult<IEnumerable<Complaint>> GetAll()
        {
            return Ok(Complaints);
        }

        [HttpGet("{id}")]
        public ActionResult<Complaint> GetById(int id)
        {
            var complaint = Complaints.FirstOrDefault(c => c.Id == id);
            if (complaint is null)
            {
                return NotFound();
            }

            return Ok(complaint);
        }

        [HttpPost]
        public ActionResult<Complaint> Create(Complaint complaint)
        {
            complaint.Id = _nextId++;
            complaint.CreatedDate = DateTime.UtcNow;
            Complaints.Add(complaint);

            return CreatedAtAction(nameof(GetById), new { id = complaint.Id }, complaint);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Complaint updated)
        {
            var complaint = Complaints.FirstOrDefault(c => c.Id == id);
            if (complaint is null)
            {
                return NotFound();
            }

            complaint.Title = updated.Title;
            complaint.Description = updated.Description;
            complaint.Category = updated.Category;
            complaint.Status = updated.Status;
            complaint.RaisedBy = updated.RaisedBy;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var complaint = Complaints.FirstOrDefault(c => c.Id == id);
            if (complaint is null)
            {
                return NotFound();
            }

            Complaints.Remove(complaint);

            return NoContent();
        }
    }
}
