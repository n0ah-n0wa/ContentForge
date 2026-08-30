namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Dashboard.Models;
using ContentForge.Application.Dashboard.Queries;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.UnitTests.Domain;
using FluentAssertions;
using NSubstitute;

public sealed class DashboardHandlerTests
{
    [Fact]
    public async Task GetDashboardQueryHandler_Admin_ReturnsStatisticsContentAndActivity()
    {
        var dashboardReadService = Substitute.For<IDashboardReadService>();
        dashboardReadService.GetContentStatisticsAsync(null, Arg.Any<CancellationToken>()).Returns(
            new DashboardContentStatisticsDto(10, 4, 2, 3, 1));
        dashboardReadService.GetRecentContentAsync(null, 5, Arg.Any<CancellationToken>()).Returns([
            new DashboardRecentContentItemDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "article",
                "Article",
                "hello",
                ContentStatus.Draft,
                DateTimeOffset.UtcNow),
        ]);
        dashboardReadService.GetRecentActivityAsync(10, Arg.Any<CancellationToken>()).Returns([
            new DashboardRecentActivityItemDto(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                AuditAction.ContentPublished,
                "ContentEntry",
                Guid.NewGuid().ToString(),
                DomainTestData.User1.Value),
        ]);

        var handler = new GetDashboardQueryHandler(
            dashboardReadService,
            ApplicationTestData.CreateCurrentUser(DomainTestData.User1, ApplicationTestData.AdministratorRole));

        var result = await handler.HandleAsync(new GetDashboardQuery(), CancellationToken.None);

        result.ContentStatistics.Should().NotBeNull();
        result.ContentStatistics!.TotalContent.Should().Be(10);
        result.RecentContent.Should().HaveCount(1);
        result.RecentActivity.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDashboardQueryHandler_Author_ScopesStatisticsToOwnContent()
    {
        var dashboardReadService = Substitute.For<IDashboardReadService>();
        dashboardReadService
            .GetContentStatisticsAsync(DomainTestData.User1, Arg.Any<CancellationToken>())
            .Returns(new DashboardContentStatisticsDto(2, 2, 0, 0, 0));
        dashboardReadService
            .GetRecentContentAsync(DomainTestData.User1, 5, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetDashboardQueryHandler(
            dashboardReadService,
            ApplicationTestData.CreateCurrentUser(DomainTestData.User1, ApplicationTestData.AuthorRole));

        var result = await handler.HandleAsync(new GetDashboardQuery(), CancellationToken.None);

        await dashboardReadService.Received(1).GetContentStatisticsAsync(DomainTestData.User1, Arg.Any<CancellationToken>());
        result.RecentActivity.Should().BeEmpty();
    }
}
