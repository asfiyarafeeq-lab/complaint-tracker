using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Tests
{
    public class ComplaintStatusesTests
    {
        [Theory]
        [InlineData("Open")]
        [InlineData("In Progress")]
        [InlineData("Resolved")]
        [InlineData("Closed")]
        public void TheFourStatuses_AreValid(string status)
        {
            Assert.True(ComplaintStatuses.IsValid(status));
        }

        [Theory]
        [InlineData("open")]
        [InlineData("OPEN")]
        [InlineData("in progress")]
        public void WrongCase_IsRejected(string status)
        {
            // Matching is deliberately case-sensitive so stored values stay uniform.
            Assert.False(ComplaintStatuses.IsValid(status));
        }

        [Theory]
        [InlineData(" Open")]
        [InlineData("Open ")]
        [InlineData("  In Progress  ")]
        public void SurroundingWhitespace_IsIgnored(string status)
        {
            Assert.True(ComplaintStatuses.IsValid(status));
        }

        [Theory]
        [InlineData("banana")]
        [InlineData("Opne")]
        [InlineData("")]
        [InlineData("   ")]
        public void AnythingElse_IsRejected(string status)
        {
            Assert.False(ComplaintStatuses.IsValid(status));
        }

        [Fact]
        public void All_ContainsEveryStatusConstant()
        {
            Assert.Equal(
                new[]
                {
                    ComplaintStatuses.Open,
                    ComplaintStatuses.InProgress,
                    ComplaintStatuses.Resolved,
                    ComplaintStatuses.Closed
                },
                ComplaintStatuses.All);
        }
    }
}
