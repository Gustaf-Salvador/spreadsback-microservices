using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.CloudWatchLogs;
using Amazon.CognitoIdentityProvider;
using Serilog;
using Serilog.Events;
using AWS.Logger.SeriLog;
using UserService.GraphQL;
using UserService.Repositories;
using UserService.Services;
using UserService.Validators;
using UserService.Models;
using FluentValidation;

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
        // Configure Serilog with CloudWatch
        ConfigureSerilog();

        // Add AWS services
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonCloudWatchLogs>();
        services.AddAWSService<IAmazonCognitoIdentityProvider>();
        services.AddScoped<DynamoDBContext>();

        // Add DynamoDB services
        services.AddScoped<IDynamoDbService, DynamoDbService>();
        services.AddScoped<IDynamoDbInitializer, DynamoDbInitializer>();

        // Add event store
        services.AddScoped<IEventStore, DynamoDbEventStore>();

        // Add domain services and repositories
        services.AddScoped<IUserRepository, DynamoDbUserRepository>();
        services.AddScoped<IUserDomainService, UserDomainService>();
        services.AddScoped<ICognitoService, CognitoService>();

        // Add validators
        services.AddScoped<IValidator<User>, UserValidator>();

        // Add GraphQL with enhanced configuration
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddErrorFilter<GraphQLErrorFilter>();

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

        // Add health checks with DynamoDB
        services.AddHealthChecks()
            .AddCheck<DynamoDbHealthCheck>("dynamodb");

        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Add request logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error
                : elapsed > 5000
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });

        app.UseCors("AllowAll");
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
            endpoints.MapHealthChecks("/health");
        });

        // Initialize DynamoDB table
        InitializeDynamoDbAsync(app.ApplicationServices, logger).GetAwaiter().GetResult();

        logger.LogInformation("UserService started successfully");
    }

    private async Task InitializeDynamoDbAsync(IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDynamoDbInitializer>();
            await initializer.InitializeAsync();
            logger.LogInformation("DynamoDB initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize DynamoDB");
            // Don't throw - let the service start but log the error
        }
    }

    private void ConfigureSerilog()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var serviceName = Configuration.GetValue<string>("ServiceName") ?? "UserService";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Amazon", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }
}