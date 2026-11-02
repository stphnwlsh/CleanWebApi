namespace CleanWebApi.Presentation.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Application;
using Authentication;
using Errors;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenApi;
using Scalar.AspNetCore;
using Serialization;
using Serilog;

[ExcludeFromCodeCoverage]
public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
    {
        #region Controllers

        _ = builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.UseApiDefaults());

        #endregion Controllers

        #region Logging

        _ = builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            var assembly = Assembly.GetEntryAssembly();

            _ = loggerConfiguration.ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
                .Enrich.WithProperty(
                    "AssemblyVersion",
                    assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
                .Enrich.WithProperty(
                    "AssemblyInformationalVersion",
                    assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        });

        #endregion Logging

        #region Serialisation

        // Problem details from outside the controllers, such as a 404 for an unknown route, use these options, so they match.
        _ = builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.UseApiDefaults());

        #endregion Serialisation

        #region OpenApi

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

        #endregion OpenApi

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

        #region Errors

        _ = builder.Services.AddProblemDetails();
        _ = builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

        #endregion Errors

        #region Project Dependencies

        _ = builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        _ = builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("MovieReviews"));
        _ = builder.Services.AddApplication();
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

        #region Errors

        _ = app.UseExceptionHandler();
        _ = app.UseStatusCodePages();

        #endregion Errors

        #region Logging

        _ = app.UseSerilogRequestLogging();

        #endregion Logging

        #region Authentication

        if (AuthenticationSettings.FromConfiguration(app.Configuration).Enabled)
        {
            _ = app.UseAuthentication();
        }

        _ = app.UseAuthorization();

        #endregion Authentication

        #region Health

        // Liveness runs no checks, so it only fails when the process can't answer. Readiness checks the database.
        _ = app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        _ = app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

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
