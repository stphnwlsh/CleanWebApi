namespace CleanWebApi.Application.Authors;

using Common.Paging;
using Entities;

public interface IAuthorsRepository
{
    public Task<Page<AuthorSummary>> GetAuthors(
        string? query,
        PageRequest page,
        CancellationToken cancellationToken);

    public Task<Author?> GetAuthorById(Guid id, CancellationToken cancellationToken);

    public Task<bool> AuthorExists(Guid id, CancellationToken cancellationToken);
}
