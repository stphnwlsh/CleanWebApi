namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

internal class AuthorConfiguration : EntityConfiguration<Author>
{
    public override void Configure(EntityTypeBuilder<Author> builder)
    {
        base.Configure(builder);

        _ = builder.Property(a => a.FirstName).UseCollation(Collations.NoCase);
        _ = builder.Property(a => a.LastName).UseCollation(Collations.NoCase);
        _ = builder.HasIndex(a => new { a.LastName, a.FirstName });

        _ = builder.HasMany(m => m.Reviews).WithOne(r => r.ReviewAuthor).HasForeignKey(r => r.ReviewAuthorId);
    }
}
