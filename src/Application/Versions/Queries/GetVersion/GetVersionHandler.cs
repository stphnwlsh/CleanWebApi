namespace CleanWebApi.Application.Versions.Queries.GetVersion;

using Entities;
using Mediator;

public class GetVersionHandler(VersionInfo version) : IRequestHandler<GetVersionQuery, VersionInfo>
{
    public ValueTask<VersionInfo> Handle(GetVersionQuery request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(version);
    }
}
