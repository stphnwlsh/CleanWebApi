namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

internal class MovieConfiguration : EntityConfiguration<Movie>
{
    public override void Configure(EntityTypeBuilder<Movie> builder)
    {
        base.Configure(builder);

        _ = builder.Property(m => m.Title).UseCollation(Collations.NoCase);
        _ = builder.HasIndex(m => m.Title);

        _ = builder.HasMany(m => m.Reviews).WithOne(r => r.ReviewedMovie).HasForeignKey(r => r.ReviewedMovieId);
    }
}
