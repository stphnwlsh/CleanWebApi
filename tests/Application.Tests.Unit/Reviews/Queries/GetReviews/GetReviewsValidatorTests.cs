namespace CleanWebApi.Application.Tests.Unit.Reviews.Queries.GetReviews;

using Application.Reviews.Queries.GetReviews;
using FluentValidation.TestHelper;
using Xunit;

public class GetReviewsValidatorTests
{
    private static readonly GetReviewsValidator Validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validator_ShouldHaveValidationErrorFor_MinStars(int minStars)
    {
        // Arrange
        var query = new GetReviewsQuery { MinStars = minStars };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(query => query.MinStars).WithErrorMessage("A minimum star rating must be between 1 and 5.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(5)]
    public void Validator_ShouldNotHaveValidationErrorFor_MinStars(int? minStars)
    {
        // Arrange
        var query = new GetReviewsQuery { MinStars = minStars };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(query => query.MinStars);
    }

    [Fact]
    public void Validator_ShouldHaveValidationErrorFor_LongQuery()
    {
        // Arrange
        var query = new GetReviewsQuery { Query = new string('a', 201) };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(query => query.Query);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validator_ShouldHaveValidationErrorFor_Limit(int limit)
    {
        // Arrange
        var query = new GetReviewsQuery { Limit = limit };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(query => query.Limit);
    }

    [Fact]
    public void Validator_ShouldHaveValidationErrorFor_Offset()
    {
        // Arrange
        var query = new GetReviewsQuery { Offset = -1 };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(query => query.Offset);
    }

    [Fact]
    public void Validator_ShouldHaveValidationErrorFor_MovieId()
    {
        // Arrange
        var query = new GetReviewsQuery { MovieId = Guid.Empty };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(query => query.MovieId);
    }

    [Fact]
    public void Validator_ShouldHaveValidationErrorFor_AuthorId()
    {
        // Arrange
        var query = new GetReviewsQuery { AuthorId = Guid.Empty };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(query => query.AuthorId);
    }

    [Fact]
    public void Validator_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var query = new GetReviewsQuery { MovieId = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Offset = 100, Limit = 50 };

        // Act
        var result = Validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
