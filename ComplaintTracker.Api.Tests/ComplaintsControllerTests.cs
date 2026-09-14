using ComplaintTracker.Api.Controllers;
using ComplaintTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintTracker.Api.Tests
{
    public class ComplaintsControllerTests
    {
        private readonly FakeComplaintRepository _repository = new();
        private readonly ComplaintsController _controller;

        public ComplaintsControllerTests()
        {
            _controller = new ComplaintsController(_repository);
        }

        private static Complaint ValidComplaint() => new()
        {
            Title = "Lift not working",
            Description = "Stuck on the second floor.",
            Category = "Infrastructure",
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
            await _controller.Get(status: "Open", category: "Plumbing", search: "leak");

            Assert.Equal("Open", _repository.LastStatus);
            Assert.Equal("Plumbing", _repository.LastCategory);
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
            var complaint = ValidComplaint();
            complaint.Id = 999;

            await _controller.Update(5, complaint);

            Assert.Equal(5, _repository.UpdatedComplaint!.Id);
        }

        [Fact]
        public async Task Update_Returns204_WhenARowChanged()
        {
            _repository.UpdateSucceeds = true;

            var response = await _controller.Update(1, ValidComplaint());

            Assert.IsType<NoContentResult>(response);
        }

        [Fact]
        public async Task Update_Returns404_WhenNoRowChanged()
        {
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
    }
}
