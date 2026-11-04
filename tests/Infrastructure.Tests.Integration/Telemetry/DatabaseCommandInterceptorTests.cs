namespace CleanWebApi.Infrastructure.Tests.Integration.Telemetry;

using System.Diagnostics;
using Infrastructure.Databases.MovieReviews;
using Infrastructure.Telemetry;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

public sealed class DatabaseCommandInterceptorTests : IDisposable
{
    private const string TestSourceName = "CleanWebApi.Infrastructure.Tests";

    private static readonly ActivitySource TestSource = new(TestSourceName);

    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly MovieReviewsDbContext context;
    private readonly ActivityListener listener;
    private readonly List<Activity> spans = [];

    public DatabaseCommandInterceptorTests()
    {
        // Other tests run at the same time, so only spans from this test's own trace are kept.
        this.listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is InfrastructureTelemetry.Name or TestSourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (this.spans)
                {
                    this.spans.Add(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(this.listener);

        this.connection.Open();

        this.context = new MovieReviewsDbContext(new DbContextOptionsBuilder<MovieReviewsDbContext>()
            .UseSqlite(this.connection)
            .AddInterceptors(DatabaseCommandInterceptor.Instance)
            .Options);

        this.context.Database.Migrate();
    }

    [Fact]
    public void Query_ShouldStartClientSpan_WithDatabaseTags()
    {
        // Arrange
        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = this.context.Movies.ToList();

        // Assert
        var span = this.SpansIn(parent).ShouldHaveSingleItem();
        span.DisplayName.ShouldBe("SELECT");
        span.Kind.ShouldBe(ActivityKind.Client);
        span.ParentSpanId.ShouldBe(parent.SpanId);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("db.system.name").ShouldBe("sqlite");
        span.GetTagItem("db.namespace").ShouldBe("main");
        span.GetTagItem("db.operation.name").ShouldBe("SELECT");
        span.GetTagItem("db.query.text").ShouldBeOfType<string>().ShouldContain("FROM \"Movies\"");
    }

    [Fact]
    public async Task QueryAsync_ShouldStartClientSpan()
    {
        // Arrange
        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = await this.context.Movies.ToListAsync();

        // Assert
        this.SpansIn(parent).ShouldHaveSingleItem().DisplayName.ShouldBe("SELECT");
    }

    [Fact]
    public async Task NonQuery_ShouldStartClientSpan_BothWays()
    {
        // Arrange
        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = this.context.Database.ExecuteSqlRaw("DELETE FROM \"Reviews\"");
        _ = await this.context.Database.ExecuteSqlRawAsync("UPDATE \"Movies\" SET \"Title\" = \"Title\"");

        // Assert
        this.SpansIn(parent).Select(s => s.DisplayName).ShouldBe(["DELETE", "UPDATE"]);
    }

    [Fact]
    public async Task Scalar_ShouldStartClientSpan_BothWays()
    {
        // Arrange
        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = this.context.Database.GetAppliedMigrations().ToList();
        _ = (await this.context.Database.GetAppliedMigrationsAsync()).ToList();

        // Assert
        this.SpansIn(parent).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task FailedCommand_ShouldMarkSpanAsError_BothWays()
    {
        // Arrange
        using var parent = TestSource.StartActivity("Test")!;

        // Act
        _ = Should.Throw<SqliteException>(() => this.context.Database.ExecuteSqlRaw("SELECT * FROM \"Missing\""));
        _ = await Should.ThrowAsync<SqliteException>(() => this.context.Database.ExecuteSqlRawAsync("SELECT * FROM \"Missing\""));

        // Assert
        var failed = this.SpansIn(parent);
        failed.Count.ShouldBe(2);

        foreach (var span in failed)
        {
            span.Status.ShouldBe(ActivityStatusCode.Error);
            span.GetTagItem("error.type").ShouldBe(typeof(SqliteException).FullName);
            span.Events.ShouldContain(e => e.Name == "exception");
        }
    }

    [Fact]
    public void Query_ShouldNotStartSpan_WhenNothingIsListening()
    {
        // Arrange
        this.listener.Dispose();

        // Act
        var movies = this.context.Movies.ToList();

        // Assert
        movies.ShouldBeEmpty();
        Activity.Current.ShouldBeNull();
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite", "sqlite")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer", "microsoft.sql_server")]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL", "postgresql")]
    [InlineData("Something.Else", "other_sql")]
    [InlineData(null, "other_sql")]
    public void SystemName_ShouldMap_Provider(string? providerName, string expected)
    {
        // Act
        var result = DatabaseCommandInterceptor.SystemName(providerName);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("SELECT * FROM \"Movies\"", "SELECT")]
    [InlineData("  insert INTO x", "INSERT")]
    [InlineData("WITH cte(x) AS (SELECT 1) SELECT x FROM cte", "WITH")]
    [InlineData("PRAGMA foreign_keys;", "PRAGMA")]
    [InlineData("VACUUM", "VACUUM")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void OperationName_ShouldReturn_FirstKeyword(string? commandText, string? expected)
    {
        // Act
        var result = DatabaseCommandInterceptor.OperationName(commandText);

        // Assert
        result.ShouldBe(expected);
    }

    public void Dispose()
    {
        this.listener.Dispose();
        this.context.Dispose();
        this.connection.Dispose();
    }

    private List<Activity> SpansIn(Activity parent)
    {
        lock (this.spans)
        {
            return [.. this.spans.Where(s => s.TraceId == parent.TraceId && s.Source.Name == InfrastructureTelemetry.Name)];
        }
    }
}
