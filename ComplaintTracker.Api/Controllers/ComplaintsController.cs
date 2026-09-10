using ComplaintTracker.Api.Models;
using ComplaintTracker.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintRepository _repository;

        public ComplaintsController(IComplaintRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Complaint>>> GetAll()
        {
            var complaints = await _repository.GetAllAsync();
            return Ok(complaints);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Complaint>> GetById(int id)
        {
            var complaint = await _repository.GetByIdAsync(id);
            if (complaint is null)
            {
                return NotFound();
            }

            return Ok(complaint);
        }

        [HttpPost]
        public async Task<ActionResult<Complaint>> Create(Complaint complaint)
        {
            complaint.CreatedDate = DateTime.UtcNow;
            complaint.Id = await _repository.CreateAsync(complaint);

            return CreatedAtAction(nameof(GetById), new { id = complaint.Id }, complaint);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Complaint updated)
        {
            updated.Id = id;

            if (!await _repository.UpdateAsync(updated))
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _repository.DeleteAsync(id))
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
