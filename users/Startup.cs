using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserService.Data;
using UserService.GraphQL;
using UserService.Repositories;
using Serilog;

namespace UserService;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add Entity Framework with In-Memory database for Lambda
        services.AddDbContext<UserDbContext>(options =>
            options.UseInMemoryDatabase("UserDatabase"));

        // Add repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Add GraphQL
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddFiltering()
            .AddSorting();

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Add health checks
        services.AddHealthChecks()
            .AddDbContextCheck<UserDbContext>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();
        
        app.UseCors("AllowAll");

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
            endpoints.MapHealthChecks("/health");
        });

        // Initialize database
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            context.Database.EnsureCreated();
        }
    }
}