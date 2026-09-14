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

        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        /// <summary>
        /// Lists complaints newest first, one page at a time. Supply status,
        /// category, and/or search (a keyword matched anywhere in the title) to
        /// narrow the results, sortOrder=asc to list oldest first, and
        /// page/pageSize to move through them.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<Complaint>>> Get(
            [FromQuery] string? status = null,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortOrder = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            bool newestFirst;
            switch ((sortOrder ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "":
                case "desc":
                    newestFirst = true;
                    break;
                case "asc":
                    newestFirst = false;
                    break;
                default:
                    ModelState.AddModelError(nameof(sortOrder), "Must be 'asc' or 'desc'.");
                    return ValidationProblem(ModelState);
            }

            if (page < 1)
            {
                ModelState.AddModelError(nameof(page), "Must be 1 or greater.");
            }

            if (pageSize < 1 || pageSize > MaxPageSize)
            {
                ModelState.AddModelError(nameof(pageSize), $"Must be between 1 and {MaxPageSize}.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _repository.SearchAsync(status, category, search, newestFirst, page, pageSize);
            return Ok(result);
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
