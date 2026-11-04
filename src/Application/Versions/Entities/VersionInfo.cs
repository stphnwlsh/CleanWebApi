namespace CleanWebApi.Application.Versions.Entities;

using System.Reflection;

/// <summary>
/// The version details of the running application.
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// The assembly file version.
    /// </summary>
    /// <example>1.1.0.0</example>
    public required string FileVersion { get; init; }

    /// <summary>
    /// The informational version, which can include a pre-release label or commit hash.
    /// </summary>
    /// <example>1.1.0+e30eba8</example>
    public required string InformationalVersion { get; init; }

    /// <summary>
    /// Reads the version attributes of the assembly the host hands over, usually its own.
    /// </summary>
    /// <param name="assembly">The assembly to read, or null for empty versions.</param>
    /// <returns>The assembly's file and informational versions.</returns>
    public static VersionInfo FromAssembly(Assembly? assembly)
    {
        return new VersionInfo
        {
            FileVersion = $"{assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version}",
            InformationalVersion = $"{assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}"
        };
    }
}
