using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.DynamoDBv2;
using Amazon.CognitoIdentityProvider;
using UserService.Services;
using UserService.Infrastructure;
using UserService.Repositories;
using UserService.Common;
using UserService.Validators;
using UserService.Models;
using UserService.GraphQL;
using UserService.GraphQL.Types;
using UserService.GraphQL.Filters;
using UserService.DTOs;
using FluentValidation;
using Serilog;
using AWS.Logger.SeriLog;

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
        // Configuration
        services.Configure<DynamoDbSettings>(Configuration.GetSection("DynamoDB"));
        services.Configure<CognitoSettings>(Configuration.GetSection("AWS:Cognito"));

        // AWS Services
        services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonCognitoIdentityProvider>();

        // Core Infrastructure
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Repositories
        services.AddScoped<IUserRepository, Repositories.DynamoDbUserRepository>();

        // Domain Services
        services.AddScoped<IUserDomainService, UserDomainService>();

        // External Services
        services.AddScoped<ICognitoService, CognitoService>();
        services.AddScoped<IDynamoDbService, DynamoDbService>();

        // Validation
        services.AddScoped<IValidator<User>, UserValidator>();
        services.AddScoped<IValidator<DTOs.CreateUserInput>, CreateUserInputValidator>();
        // Note: UpdateUserInputValidator needs to be created if it doesn't exist

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<DynamoDbHealthCheck>("dynamodb")
            .AddCheck<CognitoHealthCheck>("cognito");

        // GraphQL
        services
            .AddGraphQLServer()
            .AddQueryType<UserQueries>()
            .AddMutationType<UserMutations>()
            .AddType<UserService.GraphQL.Types.UserType>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddErrorFilter<UserService.GraphQL.Filters.GraphQLErrorFilter>();

        // HTTP Context for GraphQL
        services.AddHttpContextAccessor();

        // Background Services
        services.AddHostedService<DynamoDbInitializer>();

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseCors();

        app.UseHealthChecks("/health");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
            endpoints.MapBananaCakePop();
        });

        // Configure Serilog
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", "UserService")
            .Enrich.WithProperty("Version", GetType().Assembly.GetName().Version?.ToString() ?? "unknown");

        if (!env.IsDevelopment())
        {
            // Configure AWS CloudWatch logging with proper configuration
            loggerConfig.WriteTo.AWSSeriLog(Configuration);
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}

// Settings classes
public class DynamoDbSettings
{
    public string UserTableName { get; set; } = "Users";
    public string Region { get; set; } = "us-east-1";
    public string? ServiceURL { get; set; } // For local development
}

public class CognitoSettings
{
    public string UserPoolId { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}