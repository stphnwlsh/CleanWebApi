namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Configuration;

/// <summary>
/// The SQLite collations the model uses.
/// </summary>
internal static class Collations
{
    /// <summary>
    /// Compares ASCII letters without case, so names and titles sort and match the way people expect.
    /// </summary>
    public const string NoCase = "NOCASE";
}
