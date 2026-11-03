namespace CleanWebApi.Application.Versions.Queries.GetVersion;

using System.Reflection;
using Entities;
using Mediator;

public class GetVersionHandler : IRequestHandler<GetVersionQuery, VersionInfo>
{
    private readonly Assembly? assembly;

    /// <summary>Reads the version of the app that's running.</summary>
    public GetVersionHandler()
        : this(Assembly.GetEntryAssembly())
    {
    }

    /// <summary>Reads the version of the given assembly, so tests can choose one with known values.</summary>
    internal GetVersionHandler(Assembly? assembly)
    {
        this.assembly = assembly;
    }

    public ValueTask<VersionInfo> Handle(GetVersionQuery request, CancellationToken cancellationToken)
    {
        var version = new VersionInfo
        {
            FileVersion =
                $"{this.assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version}",
            InformationalVersion =
                $"{this.assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}"
        };

        return ValueTask.FromResult(version);
    }
}
