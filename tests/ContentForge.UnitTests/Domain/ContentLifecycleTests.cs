namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Content;
using FluentAssertions;

public sealed class ContentLifecycleTests
{
    [Theory]
    [InlineData(ContentStatus.Draft, ContentStatus.InReview, true)]
    [InlineData(ContentStatus.InReview, ContentStatus.Published, true)]
    [InlineData(ContentStatus.Published, ContentStatus.Archived, true)]
    [InlineData(ContentStatus.Published, ContentStatus.Unpublished, true)]
    [InlineData(ContentStatus.Unpublished, ContentStatus.Draft, true)]
    [InlineData(ContentStatus.Archived, ContentStatus.Draft, true)]
    [InlineData(ContentStatus.Draft, ContentStatus.Published, false)]
    [InlineData(ContentStatus.Archived, ContentStatus.Published, false)]
    public void CanTransition_MatchesSpecification(ContentStatus current, ContentStatus target, bool expected)
    {
        ContentLifecycle.CanTransition(current, target).Should().Be(expected);
    }

    [Fact]
    public void EnsureTransition_WhenInvalid_Throws()
    {
        var action = () => ContentLifecycle.EnsureTransition(ContentStatus.Draft, ContentStatus.Published);

        action.Should().Throw<ContentForge.Domain.Common.InvalidOperationDomainException>();
    }
}
