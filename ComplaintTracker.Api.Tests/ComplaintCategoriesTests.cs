using ComplaintTracker.Api.Models;

namespace ComplaintTracker.Api.Tests
{
    public class ComplaintCategoriesTests
    {
        [Theory]
        [InlineData("Hardware")]
        [InlineData("Software")]
        [InlineData("Network")]
        [InlineData("Account Access")]
        [InlineData("Email")]
        [InlineData("Printer")]
        [InlineData("Other")]
        public void TheSevenCategories_AreValid(string category)
        {
            Assert.True(ComplaintCategories.IsValid(category));
        }

        [Theory]
        [InlineData("hardware")]
        [InlineData("HARDWARE")]
        [InlineData("account access")]
        public void WrongCase_IsRejected(string category)
        {
            Assert.False(ComplaintCategories.IsValid(category));
        }

        [Theory]
        [InlineData(" Email")]
        [InlineData("Printer ")]
        [InlineData("  Account Access  ")]
        public void SurroundingWhitespace_IsIgnored(string category)
        {
            Assert.True(ComplaintCategories.IsValid(category));
        }

        [Theory]
        [InlineData("Plumbing")]
        [InlineData("Hardwre")]
        [InlineData("")]
        [InlineData("   ")]
        public void AnythingElse_IsRejected(string category)
        {
            Assert.False(ComplaintCategories.IsValid(category));
        }
    }
}
