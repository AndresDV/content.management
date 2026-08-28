using Content.Management.Api.Endpoints;
using Serilog;

namespace Content.Management.Api.Extensions;

/// <summary>Configures the HTTP request pipeline.</summary>
public static class WebApplicationExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapContentManagementEndpoints();

        app.MigrateDatabase();

        return app;
    }
}
