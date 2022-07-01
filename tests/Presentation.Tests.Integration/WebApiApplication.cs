namespace CleanWebApi.Presentation.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

internal class WebApiApplication : WebApplicationFactory<Program>
{
    private readonly string environment;

    public WebApiApplication(string environment = "local")
    {
        this.environment = environment;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _ = builder.UseEnvironment(this.environment);

        return base.CreateHost(builder);
    }
}
