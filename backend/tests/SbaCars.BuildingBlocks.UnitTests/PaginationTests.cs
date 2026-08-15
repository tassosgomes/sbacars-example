using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.UnitTests;

public class PaginationTests
{
    public class PagedRequestTests
    {
        [Fact]
        public void Constructor_WithValidValues_KeepsThem()
        {
            // Arrange & Act
            var request = new PagedRequest(page: 3, pageSize: 25);

            // Assert
            request.Page.Should().Be(3);
            request.PageSize.Should().Be(25);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Constructor_WithNonPositivePage_ClampsToOne(int page)
        {
            // Arrange & Act
            var request = new PagedRequest(page: page);

            // Assert
            request.Page.Should().Be(1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_WithNonPositivePageSize_FallsBackToDefault(int pageSize)
        {
            // Arrange & Act
            var request = new PagedRequest(pageSize: pageSize);

            // Assert
            request.PageSize.Should().Be(PagedRequest.DefaultPageSize);
        }

        [Fact]
        public void Constructor_WithPageSizeAboveMax_ClampsToMax()
        {
            // Arrange & Act
            var request = new PagedRequest(pageSize: PagedRequest.MaxPageSize + 1000);

            // Assert
            request.PageSize.Should().Be(PagedRequest.MaxPageSize);
        }

        [Theory]
        [InlineData(1, 20, 0)]
        [InlineData(2, 20, 20)]
        [InlineData(3, 10, 20)]
        public void Skip_ComputesOffsetFromPageAndSize(int page, int pageSize, int expectedSkip)
        {
            // Arrange
            var request = new PagedRequest(page, pageSize);

            // Act & Assert
            request.Skip.Should().Be(expectedSkip);
        }
    }

    public class PagedResultTests
    {
        [Fact]
        public void Constructor_WithNullItems_Throws()
        {
            // Arrange & Act
            var act = () => new PagedResult<string>(null!, 1, 10, 0);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidPage_Throws(int page)
        {
            // Arrange & Act
            var act = () => new PagedResult<string>([], page, 10, 0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithNegativeTotalCount_Throws()
        {
            // Arrange & Act
            var act = () => new PagedResult<string>([], 1, 10, -1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(20, 45, 3)]
        [InlineData(10, 100, 10)]
        [InlineData(10, 91, 10)]
        [InlineData(10, 0, 0)]
        public void TotalPages_RoundsUp(int pageSize, long totalCount, int expectedTotalPages)
        {
            // Arrange
            var result = new PagedResult<string>([], 1, pageSize, totalCount);

            // Act & Assert
            result.TotalPages.Should().Be(expectedTotalPages);
        }

        [Fact]
        public void HasNextPage_WhenOnLastPage_IsFalse()
        {
            // Arrange
            var result = new PagedResult<string>(["a", "b"], page: 3, pageSize: 20, totalCount: 45);

            // Act & Assert
            result.TotalPages.Should().Be(3);
            result.HasNextPage.Should().BeFalse();
            result.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public void HasPreviousPage_OnFirstPage_IsFalse()
        {
            // Arrange
            var result = new PagedResult<string>(["a"], page: 1, pageSize: 20, totalCount: 45);

            // Act & Assert
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public void Empty_ProducesZeroedResultForRequest()
        {
            // Arrange
            var request = new PagedRequest(page: 2, pageSize: 15);

            // Act
            var result = PagedResult<string>.Empty(request);

            // Assert
            result.Items.Should().BeEmpty();
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(15);
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasNextPage.Should().BeFalse();
        }
    }
}
