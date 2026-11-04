namespace CleanWebApi.Infrastructure.Telemetry;

using System.Buffers;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Gives every database command EF Core runs a client span, following the OpenTelemetry database conventions. EF Core
/// publishes metrics but no spans, and the OpenTelemetry EF Core instrumentation is still a prerelease package, so
/// this stands in for it until that package is stable. The SQL is the parameterised text EF Core sends, so no values
/// are recorded. When nothing is listening, no span is started.
/// </summary>
internal sealed class DatabaseCommandInterceptor : DbCommandInterceptor
{
    /// <summary>The one instance every context shares; it holds no per-request state of its own.</summary>
    public static readonly DatabaseCommandInterceptor Instance = new();

    private static readonly SearchValues<char> KeywordEnd = SearchValues.Create(" \t\r\n(;");

    private readonly ConditionalWeakTable<DbCommand, Activity> activities = [];

    #region Executing

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        this.Start(command, eventData);

        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        this.Start(command, eventData);

        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        this.Start(command, eventData);

        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        this.Start(command, eventData);

        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        this.Start(command, eventData);

        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        this.Start(command, eventData);

        return ValueTask.FromResult(result);
    }

    #endregion Executing

    #region Executed

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        this.Stop(command, null);

        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        this.Stop(command, null);

        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        this.Stop(command, null);

        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        this.Stop(command, null);

        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        this.Stop(command, null);

        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        this.Stop(command, null);

        return ValueTask.FromResult(result);
    }

    #endregion Executed

    #region Failed

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        this.Stop(command, eventData.Exception);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        this.Stop(command, eventData.Exception);

        return Task.CompletedTask;
    }

    public override void CommandCanceled(DbCommand command, CommandEndEventData eventData)
    {
        this.Stop(command, null);
    }

    public override Task CommandCanceledAsync(DbCommand command, CommandEndEventData eventData, CancellationToken cancellationToken = default)
    {
        this.Stop(command, null);

        return Task.CompletedTask;
    }

    #endregion Failed

    #region Spans

    private void Start(DbCommand command, CommandEventData eventData)
    {
        if (!InfrastructureTelemetry.ActivitySource.HasListeners())
        {
            return;
        }

        var system = SystemName(eventData.Context?.Database.ProviderName);
        var operation = OperationName(command.CommandText);
        var activity = InfrastructureTelemetry.ActivitySource.StartActivity(operation ?? system, ActivityKind.Client);

        if (activity is null)
        {
            return;
        }

        if (activity.IsAllDataRequested)
        {
            _ = activity.SetTag("db.system.name", system);
            _ = activity.SetTag("db.namespace", command.Connection?.Database);
            _ = activity.SetTag("db.operation.name", operation);
            _ = activity.SetTag("db.query.text", command.CommandText);
        }

        this.activities.AddOrUpdate(command, activity);
    }

    private void Stop(DbCommand command, Exception? exception)
    {
        if (!this.activities.TryGetValue(command, out var activity))
        {
            return;
        }

        _ = this.activities.Remove(command);

        if (exception is not null)
        {
            _ = activity.SetTag("error.type", exception.GetType().FullName);
            _ = activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            _ = activity.AddException(exception);
        }

        activity.Stop();
    }

    /// <summary>Maps the EF Core provider to the <c>db.system.name</c> the conventions use.</summary>
    internal static string SystemName(string? providerName)
    {
        return providerName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" => "sqlite",
            "Microsoft.EntityFrameworkCore.SqlServer" => "microsoft.sql_server",
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "postgresql",
            _ => "other_sql"
        };
    }

    /// <summary>The SQL keyword the command starts with, such as <c>SELECT</c>, or null when there isn't one.</summary>
    internal static string? OperationName(string? commandText)
    {
        var text = commandText.AsSpan().TrimStart();
        var end = text.IndexOfAny(KeywordEnd);
        var keyword = end < 0 ? text : text[..end];

        return keyword.IsEmpty ? null : keyword.ToString().ToUpperInvariant();
    }

    #endregion Spans
}
