using System.Security.Claims;
using ComplaintTracker.Api.Controllers;
using ComplaintTracker.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintTracker.Api.Tests
{
    public class ComplaintsControllerTests
    {
        private readonly FakeComplaintRepository _repository = new();
        private readonly FakeUserRepository _users = new();
        private readonly ComplaintsController _controller;

        private const int AdminId = 1;
        private const int UserId = 2;
        private const int OtherUserId = 3;

        /// <summary>Puts a signed-in account behind the controller.</summary>
        private void SignIn(int userId, string username, string role)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                },
                authenticationType: "Test");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        public ComplaintsControllerTests()
        {
            _controller = new ComplaintsController(_repository, _users);

            // Most tests only exercise the listing rules, so they run as an
            // Admin, who can see everything. Ownership tests sign in again.
            SignIn(AdminId, "admin", UserRoles.Admin);
        }

        private static Complaint ValidComplaint() => new()
        {
            Title = "Lift not working",
            Description = "Stuck on the second floor.",
            Category = ComplaintCategories.Hardware,
            Status = ComplaintStatuses.Open,
            RaisedBy = "asfiya"
        };

        [Fact]
        public async Task Get_WithNoArguments_DefaultsToNewestFirstPageOneOfTwenty()
        {
            var response = await _controller.Get();

            Assert.IsType<OkObjectResult>(response.Result);
            Assert.True(_repository.SearchWasCalled);
            Assert.Equal(ComplaintSortField.CreatedDate, _repository.LastSortField);
            Assert.Equal(SortDirection.Descending, _repository.LastSortDirection);
            Assert.Equal(1, _repository.LastPage);
            Assert.Equal(20, _repository.LastPageSize);
        }

        [Fact]
        public async Task Get_PassesFiltersThroughUntouched()
        {
            await _controller.Get(status: "Open", category: "Network", search: "leak");

            Assert.Equal("Open", _repository.LastStatus);
            Assert.Equal("Network", _repository.LastCategory);
            Assert.Equal("leak", _repository.LastSearch);
        }

        [Theory]
        [InlineData("title", ComplaintSortField.Title)]
        [InlineData("TITLE", ComplaintSortField.Title)]
        [InlineData("createdDate", ComplaintSortField.CreatedDate)]
        [InlineData(null, ComplaintSortField.CreatedDate)]
        public async Task Get_ResolvesSortBy(string? sortBy, ComplaintSortField expected)
        {
            await _controller.Get(sortBy: sortBy);

            Assert.Equal(expected, _repository.LastSortField);
        }

        [Theory]
        [InlineData("asc", SortDirection.Ascending)]
        [InlineData("desc", SortDirection.Descending)]
        [InlineData(null, SortDirection.Descending)]
        public async Task Get_ResolvesSortOrder(string? sortOrder, SortDirection expected)
        {
            await _controller.Get(sortOrder: sortOrder);

            Assert.Equal(expected, _repository.LastSortDirection);
        }

        [Fact]
        public async Task Get_RejectsUnknownSortBy()
        {
            var response = await _controller.Get(sortBy: "raisedBy");

            Assert.IsType<ObjectResult>(response.Result);
            Assert.False(_repository.SearchWasCalled);
        }

        [Fact]
        public async Task Get_RejectsUnknownSortOrder()
        {
            var response = await _controller.Get(sortOrder: "sideways");

            Assert.IsType<ObjectResult>(response.Result);
            Assert.False(_repository.SearchWasCalled);
        }

        [Fact]
        public async Task Get_ReportsBadSortByAndSortOrderTogether()
        {
            await _controller.Get(sortBy: "raisedBy", sortOrder: "sideways");

            Assert.True(_controller.ModelState.ContainsKey("sortBy"));
            Assert.True(_controller.ModelState.ContainsKey("sortOrder"));
        }

        [Theory]
        [InlineData("open")]
        [InlineData("Opne")]
        [InlineData("banana")]
        public async Task Get_RejectsInvalidStatusFilter(string status)
        {
            // Without this the caller gets an empty page, which reads the same
            // as nothing matched and hides the mistake.
            var response = await _controller.Get(status: status);

            Assert.IsType<ObjectResult>(response.Result);
            Assert.False(_repository.SearchWasCalled);
        }

        [Fact]
        public async Task Get_AllowsBlankStatus_MeaningNoFilter()
        {
            var response = await _controller.Get(status: "");

            Assert.IsType<OkObjectResult>(response.Result);
            Assert.True(_repository.SearchWasCalled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Get_RejectsPageBelowOne(int page)
        {
            var response = await _controller.Get(page: page);

            Assert.IsType<ObjectResult>(response.Result);
            Assert.False(_repository.SearchWasCalled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task Get_RejectsPageSizeOutsideRange(int pageSize)
        {
            // The upper bound stops one request pulling the whole table.
            var response = await _controller.Get(pageSize: pageSize);

            Assert.IsType<ObjectResult>(response.Result);
            Assert.False(_repository.SearchWasCalled);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task Get_AcceptsPageSizeAtTheBoundaries(int pageSize)
        {
            var response = await _controller.Get(pageSize: pageSize);

            Assert.IsType<OkObjectResult>(response.Result);
            Assert.Equal(pageSize, _repository.LastPageSize);
        }

        [Fact]
        public async Task GetById_ReturnsTheComplaint_WhenFound()
        {
            _repository.ComplaintToReturn = ValidComplaint();

            var response = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(response.Result);
        }

        [Fact]
        public async Task GetById_Returns404_WhenMissing()
        {
            _repository.ComplaintToReturn = null;

            var response = await _controller.GetById(999);

            Assert.IsType<NotFoundResult>(response.Result);
        }

        [Fact]
        public async Task Create_StampsCreatedDateAndIdFromTheDatabase()
        {
            var before = DateTime.UtcNow;
            _repository.NewId = 7;

            var response = await _controller.Create(ValidComplaint());

            var created = Assert.IsType<CreatedAtActionResult>(response.Result);
            var complaint = Assert.IsType<Complaint>(created.Value);

            Assert.Equal(7, complaint.Id);
            Assert.InRange(complaint.CreatedDate, before, DateTime.UtcNow);
        }

        [Fact]
        public async Task Create_IgnoresAnyCreatedDateTheCallerSent()
        {
            var complaint = ValidComplaint();
            complaint.CreatedDate = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await _controller.Create(complaint);

            Assert.NotEqual(1999, _repository.CreatedComplaint!.CreatedDate.Year);
        }

        [Fact]
        public async Task Update_TakesTheIdFromTheRouteNotTheBody()
        {
            _repository.ComplaintToReturn = ValidComplaint();

            var complaint = ValidComplaint();
            complaint.Id = 999;

            await _controller.Update(5, complaint);

            Assert.Equal(5, _repository.UpdatedComplaint!.Id);
        }

        [Fact]
        public async Task Update_Returns204_WhenARowChanged()
        {
            _repository.ComplaintToReturn = ValidComplaint();
            _repository.UpdateSucceeds = true;

            var response = await _controller.Update(1, ValidComplaint());

            Assert.IsType<NoContentResult>(response);
        }

        [Fact]
        public async Task Update_Returns404_WhenNoRowChanged()
        {
            _repository.ComplaintToReturn = ValidComplaint();
            _repository.UpdateSucceeds = false;

            var response = await _controller.Update(999, ValidComplaint());

            Assert.IsType<NotFoundResult>(response);
        }

        [Fact]
        public async Task Delete_Returns204_WhenARowWasRemoved()
        {
            _repository.DeleteSucceeds = true;

            var response = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(response);
            Assert.Equal(1, _repository.DeletedId);
        }

        [Fact]
        public async Task Delete_Returns404_WhenNothingMatched()
        {
            _repository.DeleteSucceeds = false;

            var response = await _controller.Delete(999);

            Assert.IsType<NotFoundResult>(response);
        }

        // ---------- who sees what ----------

        [Fact]
        public async Task Get_AsUser_IsRestrictedToTheirOwnTickets()
        {
            SignIn(UserId, "asfiya", UserRoles.User);

            await _controller.Get();

            Assert.Equal(UserId, _repository.LastRaisedByUserId);
        }

        [Theory]
        [InlineData(UserRoles.Agent)]
        [InlineData(UserRoles.Admin)]
        public async Task Get_AsStaff_SeesEveryTicket(string role)
        {
            SignIn(AdminId, "staff", role);

            await _controller.Get();

            // Null means no owner restriction at all.
            Assert.Null(_repository.LastRaisedByUserId);
        }

        [Fact]
        public async Task GetById_AsUser_ReturnsTheirOwnTicket()
        {
            SignIn(UserId, "asfiya", UserRoles.User);
            var mine = ValidComplaint();
            mine.RaisedByUserId = UserId;
            _repository.ComplaintToReturn = mine;

            var response = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(response.Result);
        }

        [Fact]
        public async Task GetById_AsUser_Returns404ForSomeoneElsesTicket()
        {
            // 404 rather than 403, so the answer does not confirm it exists.
            SignIn(UserId, "asfiya", UserRoles.User);
            var theirs = ValidComplaint();
            theirs.RaisedByUserId = OtherUserId;
            _repository.ComplaintToReturn = theirs;

            var response = await _controller.GetById(1);

            Assert.IsType<NotFoundResult>(response.Result);
        }

        [Fact]
        public async Task GetById_AsAgent_ReturnsAnyonesTicket()
        {
            SignIn(AdminId, "agent", UserRoles.Agent);
            var theirs = ValidComplaint();
            theirs.RaisedByUserId = OtherUserId;
            _repository.ComplaintToReturn = theirs;

            var response = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(response.Result);
        }

        [Fact]
        public async Task Update_AsUser_Returns404ForSomeoneElsesTicket()
        {
            SignIn(UserId, "asfiya", UserRoles.User);
            var theirs = ValidComplaint();
            theirs.RaisedByUserId = OtherUserId;
            _repository.ComplaintToReturn = theirs;

            var response = await _controller.Update(1, ValidComplaint());

            Assert.IsType<NotFoundResult>(response);
        }

        // ---------- ownership on create ----------

        [Fact]
        public async Task Create_TakesTheOwnerFromTheToken()
        {
            SignIn(UserId, "asfiya", UserRoles.User);

            await _controller.Create(ValidComplaint());

            Assert.Equal(UserId, _repository.CreatedComplaint!.RaisedByUserId);
            Assert.Equal("asfiya", _repository.CreatedComplaint.RaisedBy);
        }

        [Fact]
        public async Task Create_IgnoresAnyRaisedByTheCallerSent()
        {
            SignIn(UserId, "asfiya", UserRoles.User);
            var complaint = ValidComplaint();
            complaint.RaisedBy = "somebody-else";
            complaint.RaisedByUserId = OtherUserId;

            await _controller.Create(complaint);

            Assert.Equal("asfiya", _repository.CreatedComplaint!.RaisedBy);
            Assert.Equal(UserId, _repository.CreatedComplaint.RaisedByUserId);
        }

        // ---------- assignment ----------

        [Fact]
        public async Task Assign_PointsTheTicketAtAnAgent()
        {
            _repository.ComplaintToReturn = ValidComplaint();
            _users.Add(5, "nawaz", UserRoles.Agent);

            var response = await _controller.Assign(1, new AssignRequest { AssignedToUserId = 5 });

            Assert.IsType<NoContentResult>(response);
            Assert.Equal(5, _repository.AssignedUserId);
            Assert.Equal("nawaz", _repository.AssignedUsername);
        }

        [Fact]
        public async Task Assign_AcceptsAnAdminAsTheAssignee()
        {
            _repository.ComplaintToReturn = ValidComplaint();
            _users.Add(5, "boss", UserRoles.Admin);

            var response = await _controller.Assign(1, new AssignRequest { AssignedToUserId = 5 });

            Assert.IsType<NoContentResult>(response);
        }

        [Fact]
        public async Task Assign_RejectsAPlainUserAsTheAssignee()
        {
            // Assigning to someone who cannot work tickets strands the work.
            _repository.ComplaintToReturn = ValidComplaint();
            _users.Add(5, "someone", UserRoles.User);

            var response = await _controller.Assign(1, new AssignRequest { AssignedToUserId = 5 });

            Assert.IsType<ObjectResult>(response);
            Assert.False(_repository.AssignWasCalled);
        }

        [Fact]
        public async Task Assign_RejectsAnAccountThatDoesNotExist()
        {
            _repository.ComplaintToReturn = ValidComplaint();

            var response = await _controller.Assign(1, new AssignRequest { AssignedToUserId = 999 });

            Assert.IsType<ObjectResult>(response);
            Assert.False(_repository.AssignWasCalled);
        }

        [Fact]
        public async Task Assign_Returns404ForATicketThatDoesNotExist()
        {
            _repository.ComplaintToReturn = null;

            var response = await _controller.Assign(999, new AssignRequest { AssignedToUserId = 5 });

            Assert.IsType<NotFoundResult>(response);
            Assert.False(_repository.AssignWasCalled);
        }

        [Fact]
        public async Task Assign_WithNullClearsTheAssignment()
        {
            _repository.ComplaintToReturn = ValidComplaint();

            var response = await _controller.Assign(1, new AssignRequest { AssignedToUserId = null });

            Assert.IsType<NoContentResult>(response);
            Assert.True(_repository.AssignWasCalled);
            Assert.Null(_repository.AssignedUserId);
            Assert.Null(_repository.AssignedUsername);
        }

        // ---------- the assigned-to-me filter ----------

        [Fact]
        public async Task Get_WithAssignedToMe_FiltersToTheCaller()
        {
            SignIn(7, "nawaz", UserRoles.Agent);

            await _controller.Get(assignedToMe: true);

            Assert.Equal(7, _repository.LastAssignedToUserId);
        }

        [Fact]
        public async Task Get_WithoutAssignedToMe_PlacesNoAssignmentFilter()
        {
            SignIn(7, "nawaz", UserRoles.Agent);

            await _controller.Get();

            Assert.Null(_repository.LastAssignedToUserId);
        }

        // ---------- category filter ----------

        [Theory]
        [InlineData("hardware")]
        [InlineData("Plumbing")]
        [InlineData("nonsense")]
        public async Task Get_RejectsInvalidCategoryFilter(string category)
        {
            var response = await _controller.Get(category: category);

            Assert.IsType<ObjectResult>(response.Result);
            Assert.False(_repository.SearchWasCalled);
        }

        [Fact]
        public async Task Get_AcceptsAValidCategoryFilter()
        {
            var response = await _controller.Get(category: ComplaintCategories.Network);

            Assert.IsType<OkObjectResult>(response.Result);
            Assert.Equal("Network", _repository.LastCategory);
        }
    }
}
