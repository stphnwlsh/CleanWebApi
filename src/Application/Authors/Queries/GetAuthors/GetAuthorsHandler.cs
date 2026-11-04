namespace CleanWebApi.Application.Authors.Queries.GetAuthors;

using Common.Paging;
using Entities;
using Mediator;

public class GetAuthorsHandler(IAuthorsRepository repository) : IRequestHandler<GetAuthorsQuery, Page<AuthorSummary>>
{
    public async ValueTask<Page<AuthorSummary>> Handle(GetAuthorsQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetAuthors(request.Query, new PageRequest(request.Offset, request.Limit), cancellationToken);
    }
}
