namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Configuration;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

internal class ReviewConfiguration : EntityConfiguration<Review>
{
    public override void Configure(EntityTypeBuilder<Review> builder)
    {
        base.Configure(builder);

        _ = builder.Property(r => r.Text).HasMaxLength(1000);
    }
}
