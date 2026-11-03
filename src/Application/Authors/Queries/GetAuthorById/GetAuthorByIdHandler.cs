namespace CleanWebApi.Application.Authors.Queries.GetAuthorById;

using Common.Enums;
using Common.Exceptions;
using Entities;
using Mediator;

public class GetAuthorByIdHandler(IAuthorsRepository repository) : IRequestHandler<GetAuthorByIdQuery, Author>
{
    public async ValueTask<Author> Handle(GetAuthorByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await repository.GetAuthorById(request.Id, cancellationToken);

        NotFoundException.ThrowIfNull(result, EntityType.Author);

        return result;
    }
}
