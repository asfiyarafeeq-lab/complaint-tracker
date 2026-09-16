using System.Security.Claims;
using ComplaintTracker.Api.Models;
using ComplaintTracker.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly IComplaintRepository _repository;

        public ComplaintsController(IComplaintRepository repository)
        {
            _repository = repository;
        }

        /// <summary>The id of the account making this request.</summary>
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private string CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        /// <summary>
        /// Agents and Admins work across the whole queue; a plain User only ever
        /// sees their own tickets.
        /// </summary>
        private bool CanSeeEveryTicket =>
            User.IsInRole(UserRoles.Agent) || User.IsInRole(UserRoles.Admin);

        /// <summary>
        /// The owner to filter by, or null to place no restriction.
        /// </summary>
        private int? OwnerRestriction => CanSeeEveryTicket ? null : CurrentUserId;

        /// <summary>
        /// Lists complaints newest first, one page at a time. Users see their
        /// own; Agents and Admins see all. Supply status, category, and/or
        /// search to narrow the results, sortBy/sortOrder to change the
        /// ordering, and page/pageSize to move through them.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<Complaint>>> Get(
            [FromQuery] string? status = null,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            // Both sort inputs are resolved against a fixed set here, so the
            // repository never sees caller text in its ORDER BY clause.
            var sortField = ComplaintSortField.CreatedDate;
            switch ((sortBy ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "":
                case "createddate":
                    sortField = ComplaintSortField.CreatedDate;
                    break;
                case "title":
                    sortField = ComplaintSortField.Title;
                    break;
                default:
                    ModelState.AddModelError(nameof(sortBy), "Must be 'createdDate' or 'title'.");
                    break;
            }

            var sortDirection = SortDirection.Descending;
            switch ((sortOrder ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "":
                case "desc":
                    sortDirection = SortDirection.Descending;
                    break;
                case "asc":
                    sortDirection = SortDirection.Ascending;
                    break;
                default:
                    ModelState.AddModelError(nameof(sortOrder), "Must be 'asc' or 'desc'.");
                    break;
            }

            // An unknown status would otherwise return an empty page, which reads
            // the same as "nothing matched" and hides the typo.
            if (!string.IsNullOrWhiteSpace(status) && !ComplaintStatuses.IsValid(status))
            {
                ModelState.AddModelError(
                    nameof(status),
                    $"Must be one of: {string.Join(", ", ComplaintStatuses.All)}.");
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

            var result = await _repository.SearchAsync(
                status, category, search, OwnerRestriction, sortField, sortDirection, page, pageSize);

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

            // Someone else's ticket is reported as missing rather than
            // forbidden, so the response does not confirm that it exists.
            if (!CanSeeEveryTicket && complaint.RaisedByUserId != CurrentUserId)
            {
                return NotFound();
            }

            return Ok(complaint);
        }

        [HttpPost]
        public async Task<ActionResult<Complaint>> Create(Complaint complaint)
        {
            complaint.CreatedDate = DateTime.UtcNow;

            // Ownership comes from the token: a caller cannot raise a ticket in
            // somebody else's name by putting it in the body.
            complaint.RaisedBy = CurrentUsername;
            complaint.RaisedByUserId = CurrentUserId;

            complaint.Id = await _repository.CreateAsync(complaint);

            return CreatedAtAction(nameof(GetById), new { id = complaint.Id }, complaint);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Complaint updated)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            if (!CanSeeEveryTicket && existing.RaisedByUserId != CurrentUserId)
            {
                return NotFound();
            }

            updated.Id = id;

            if (!await _repository.UpdateAsync(updated))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Removing a ticket destroys the record of it, so this is kept to
        /// Admins. Everyone else closes tickets instead.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
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
