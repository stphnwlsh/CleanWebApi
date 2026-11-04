namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Mapping;

using Riok.Mapperly.Abstractions;
using AppAuthor = Application.Authors.Entities.Author;
using AppMovie = Application.Movies.Entities.Movie;
using AppReview = Application.Reviews.Entities.Review;
using AppReviewAuthor = Application.Authors.Entities.ReviewAuthor;
using AppReviewedMovie = Application.Movies.Entities.ReviewedMovie;
using InfraAuthor = Models.Author;
using InfraMovie = Models.Movie;
using InfraReview = Models.Review;

[Mapper]
internal partial class MovieReviewsMapper
{
    [MapperIgnoreSource(nameof(InfraAuthor.DateCreated))]
    [MapperIgnoreSource(nameof(InfraAuthor.DateModified))]
    public partial AppAuthor ToDomain(InfraAuthor author);

    [MapperIgnoreSource(nameof(InfraAuthor.Reviews))]
    [MapperIgnoreSource(nameof(InfraAuthor.DateCreated))]
    [MapperIgnoreSource(nameof(InfraAuthor.DateModified))]
    public partial AppReviewAuthor ToReviewAuthor(InfraAuthor author);

    [MapperIgnoreSource(nameof(InfraMovie.DateCreated))]
    [MapperIgnoreSource(nameof(InfraMovie.DateModified))]
    public partial AppMovie ToDomain(InfraMovie movie);

    [MapperIgnoreSource(nameof(InfraMovie.Reviews))]
    [MapperIgnoreSource(nameof(InfraMovie.DateCreated))]
    [MapperIgnoreSource(nameof(InfraMovie.DateModified))]
    public partial AppReviewedMovie ToReviewedMovie(InfraMovie movie);

    [MapperIgnoreSource(nameof(InfraReview.ReviewAuthorId))]
    [MapperIgnoreSource(nameof(InfraReview.ReviewedMovieId))]
    [MapperIgnoreSource(nameof(InfraReview.DateCreated))]
    [MapperIgnoreSource(nameof(InfraReview.DateModified))]
    public partial AppReview ToDomain(InfraReview review);
}
