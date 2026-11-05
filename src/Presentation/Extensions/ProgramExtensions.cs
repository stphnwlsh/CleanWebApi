namespace CleanWebApi.Presentation.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Application;
using Application.Common.Telemetry;
using Application.Versions.Entities;
#if includeAuth
using Authentication;
#endif
using Errors;
using Infrastructure;
#if includeAuth
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenApi;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serialization;
using Serilog;
using Telemetry;

[ExcludeFromCodeCoverage]
public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
    {
        #region Controllers

        _ = builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.UseApiDefaults());

        #endregion Controllers

        #region Telemetry

        var assembly = typeof(ProgramExtensions).Assembly;
        var telemetry = TelemetrySettings.FromConfiguration(builder.Configuration, builder.Environment.EnvironmentName, assembly);

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(telemetry.ResourceAttributes))
            .WithTracing(tracing => tracing
                .AddSource(ApplicationTelemetry.Name)
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Probes call the health endpoints every few seconds, so tracing them would bury everything else.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options => options.RecordException = true))
            .WithMetrics(metrics => metrics
#if includeAuth
                .AddMeter(
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.AspNetCore.Authentication",
                    "Microsoft.AspNetCore.Authorization")
#else
                .AddMeter("Microsoft.EntityFrameworkCore")
#endif
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .SetExemplarFilter(ExemplarFilterType.TraceBased));

        // The exporters read OTEL_EXPORTER_OTLP_ENDPOINT and the other OTEL_* settings themselves.
        if (telemetry.Enabled)
        {
            _ = openTelemetry.UseOtlpExporter();
        }

        #endregion Telemetry

        #region Logging

        _ = builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            _ = loggerConfiguration.ReadFrom.Configuration(configuration)
                .Enrich.WithProperty(
                    "AssemblyVersion",
                    assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
                .Enrich.WithProperty(
                    "AssemblyInformationalVersion",
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

            // Logs leave over OTLP too, carrying the trace and span IDs of whatever was running when they were written.
            if (telemetry.Enabled)
            {
                _ = loggerConfiguration.WriteTo.OpenTelemetry(
                    options =>
                    {
                        options.ResourceAttributes = telemetry.ResourceAttributes;
                        options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
                    },
                    key => configuration[key]);
            }
        });

        #endregion Logging

        #region Serialisation

        // Problem details from outside the controllers, such as a 404 for an unknown route, use these options, so they match.
        _ = builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.UseApiDefaults());

        #endregion Serialisation

        #region OpenApi

#if includeAuth
        var authentication = AuthenticationSettings.FromConfiguration(builder.Configuration);

        _ = builder.Services.AddOpenApi(options =>
        {
            _ = options.AddDocumentTransformer<DocumentInfoTransformer>();

            if (authentication.Enabled)
            {
                _ = options.AddDocumentTransformer<BearerSecurityTransformer>();
                _ = options.AddOperationTransformer<BearerSecurityTransformer>();
            }
        });
#else
        _ = builder.Services.AddOpenApi(options => options.AddDocumentTransformer<DocumentInfoTransformer>());
#endif

        #endregion OpenApi

#if includeAuth
        #region Authentication

        if (authentication.Enabled)
        {
            _ = builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authentication.Authority;
                    options.Audience = authentication.Audience;
                    options.MapInboundClaims = false;
                });
            _ = builder.Services.AddAuthorization();
        }
        else
        {
            // Controllers always ask for authorization, so without an authority every request is let through.
            _ = builder.Services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
        }

        #endregion Authentication

#endif
        #region Errors

        _ = builder.Services.AddProblemDetails();
        _ = builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

        #endregion Errors

        #region Project Dependencies

        _ = builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        _ = builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("MovieReviews"));
        _ = builder.Services.AddApplication();
        _ = builder.Services.AddSingleton(VersionInfo.FromAssembly(typeof(ProgramExtensions).Assembly));
        _ = builder.Services.AddHealthChecks().AddInfrastructureHealthChecks("ready");

        #endregion Project Dependencies

        return builder;
    }

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        #region Database

        _ = app.Services.InitialiseInfrastructure(app.Configuration.GetValue<bool>("Database:SeedSampleData"));

        #endregion Database

        #region Security

        // HSTS tells browsers to only use HTTPS from then on, so it's left off for local runs.
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("local"))
        {
            _ = app.UseHsts();
        }

        _ = app.UseHttpsRedirection();

        #endregion Security

        #region Logging

        // Before the exception handler, so each request is logged once with the status code the client gets.
        _ = app.UseSerilogRequestLogging();

        #endregion Logging

        #region Errors

        _ = app.UseExceptionHandler();
        _ = app.UseStatusCodePages();

        #endregion Errors

#if includeAuth
        #region Authentication

        if (AuthenticationSettings.FromConfiguration(app.Configuration).Enabled)
        {
            _ = app.UseAuthentication();
        }

        _ = app.UseAuthorization();

        #endregion Authentication

#endif
        #region Health

        // Liveness runs no checks, so it only fails when the process can't answer. Readiness checks the database.
#if includeAuth
        _ = app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        _ = app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
#else
        _ = app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        _ = app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
#endif

        #endregion Health

        #region Controllers

        _ = app.MapControllers();

        #endregion Controllers

        #region OpenApi

        _ = app.MapOpenApi();
        _ = app.MapScalarApiReference(options => options
            .WithTitle("CleanWebApi API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .HideDeveloperTools()
            .DisableAgent()
            .DisableMcp()
            .DisableTelemetry());

        #endregion OpenApi

        return app;
    }
}
