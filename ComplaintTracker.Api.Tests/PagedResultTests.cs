using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Tests
{
    public class PagedResultTests
    {
        [Theory]
        [InlineData(0, 20, 0)]   // nothing matched
        [InlineData(4, 20, 1)]   // fits on one page
        [InlineData(4, 2, 2)]    // exact multiple
        [InlineData(5, 2, 3)]    // remainder needs a page of its own
        [InlineData(1, 100, 1)]
        public void TotalPages_RoundsUpToCoverEveryRow(int totalCount, int pageSize, int expected)
        {
            var result = new PagedResult<Complaint> { PageSize = pageSize, TotalCount = totalCount };

            Assert.Equal(expected, result.TotalPages);
        }

        [Fact]
        public void TotalPages_IsZero_WhenPageSizeIsZero()
        {
            // Guards the division rather than throwing.
            var result = new PagedResult<Complaint> { PageSize = 0, TotalCount = 10 };

            Assert.Equal(0, result.TotalPages);
        }

        [Fact]
        public void FirstPage_HasNextButNoPrevious()
        {
            var result = new PagedResult<Complaint> { Page = 1, PageSize = 2, TotalCount = 5 };

            Assert.False(result.HasPrevious);
            Assert.True(result.HasNext);
        }

        [Fact]
        public void MiddlePage_HasBoth()
        {
            var result = new PagedResult<Complaint> { Page = 2, PageSize = 2, TotalCount = 5 };

            Assert.True(result.HasPrevious);
            Assert.True(result.HasNext);
        }

        [Fact]
        public void LastPage_HasPreviousButNoNext()
        {
            var result = new PagedResult<Complaint> { Page = 3, PageSize = 2, TotalCount = 5 };

            Assert.True(result.HasPrevious);
            Assert.False(result.HasNext);
        }

        [Fact]
        public void EmptyResult_HasNeither()
        {
            var result = new PagedResult<Complaint> { Page = 1, PageSize = 20, TotalCount = 0 };

            Assert.False(result.HasPrevious);
            Assert.False(result.HasNext);
        }
    }
}
