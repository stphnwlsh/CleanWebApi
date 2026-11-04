namespace CleanWebApi.Application.Common.Paging;

/// <summary>
/// The slice of a list a use case asks a repository for.
/// </summary>
/// <param name="Offset">How many items to skip. 0 is the first page.</param>
/// <param name="Limit">The most items to return.</param>
public record PageRequest(int Offset, int Limit);
