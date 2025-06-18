using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpreadsBack.CommonServices.Infrastructure.Persistence;
using SpreadsBack.CommonServices.Infrastructure.Repositories;
using SpreadsBack.CommonServices.Infrastructure.Authentication;
using SpreadsBack.CommonServices.Infrastructure.EventTracking;
using SpreadsBack.CommonServices.Infrastructure.Messaging;
using SpreadsBack.CommonServices.Infrastructure.Logging;
using Amazon.CognitoIdentityProvider;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon;
using Amazon.Lambda.Core;
using System.Reflection;

namespace SpreadsBack.CommonServices.Infrastructure.Configuration;

/// <summary>
/// Extensões para configuração de serviços comuns
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona serviços comuns dos microserviços
    /// </summary>
    public static IServiceCollection AddCommonServices(
        this IServiceCollection services,
        Assembly applicationAssembly)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(applicationAssembly);
        
        // Repositórios base
        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

        // Event tracking
        services.AddScoped<IEventTracker, EfEventTracker>();

        return services;
    }

    /// <summary>
    /// Adiciona DbContext personalizado
    /// </summary>
    public static IServiceCollection AddDbContext<TContext>(
        this IServiceCollection services,
        string connectionString)
        where TContext : BaseDbContext
    {
        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<BaseDbContext>(provider => provider.GetRequiredService<TContext>());

        return services;
    }

    /// <summary>
    /// Adiciona configuração de database settings
    /// </summary>
    public static IServiceCollection AddDatabaseSettings(
        this IServiceCollection services,
        Action<DatabaseSettings> configureOptions)
    {
        var settings = new DatabaseSettings();
        configureOptions(settings);
        services.AddSingleton(settings);

        return services;
    }

    /// <summary>
    /// Adiciona serviços AWS comuns
    /// </summary>
    public static IServiceCollection AddAwsServices(
        this IServiceCollection services,
        string region = "us-east-1")
    {
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        services.AddSingleton<IAmazonCognitoIdentityProvider>(_ => 
            new AmazonCognitoIdentityProviderClient(regionEndpoint));
            
        services.AddSingleton<IAmazonSimpleNotificationService>(_ => 
            new AmazonSimpleNotificationServiceClient(regionEndpoint));
            
        services.AddSingleton<IAmazonSQS>(_ => 
            new AmazonSQSClient(regionEndpoint));

        return services;
    }

    /// <summary>
    /// Adiciona serviços de autenticação Cognito
    /// </summary>
    public static IServiceCollection AddCognitoAuthentication(
        this IServiceCollection services,
        Action<CognitoSettings> configureOptions)
    {
        var settings = new CognitoSettings();
        configureOptions(settings);
        services.AddSingleton(settings);
        services.AddScoped<ICognitoService, CognitoService>();

        return services;
    }

    /// <summary>
    /// Adiciona event publisher SNS
    /// </summary>
    public static IServiceCollection AddSnsEventPublisher(
        this IServiceCollection services,
        Action<SnsSettings> configureOptions)
    {
        var settings = new SnsSettings();
        configureOptions(settings);
        services.AddSingleton(settings);
        services.AddScoped<IEventPublisher, SnsEventPublisher>();

        return services;
    }

    /// <summary>
    /// Adiciona logging para Lambda
    /// </summary>
    public static IServiceCollection AddLambdaLogging(
        this IServiceCollection services,
        ILambdaLogger lambdaLogger)
    {
        services.AddSingleton<ILoggerFactory>(new LambdaLoggerFactory(lambdaLogger));
        services.AddSingleton(typeof(ILogger<>), typeof(LambdaLoggerAdapter<>));

        return services;
    }

    /// <summary>
    /// Configuração completa para microserviços Lambda
    /// </summary>
    public static IServiceCollection AddMicroserviceDefaults<TDbContext>(
        this IServiceCollection services,
        Assembly applicationAssembly,
        string connectionString,
        string awsRegion = "us-east-1")
        where TDbContext : BaseDbContext
    {
        services.AddCommonServices(applicationAssembly);
        services.AddDbContext<TDbContext>(connectionString);
        services.AddAwsServices(awsRegion);
        
        services.AddDatabaseSettings(settings =>
        {
            settings.ConnectionString = connectionString;
            settings.EnableDetailedErrors = false;
            settings.CommandTimeout = 30;
        });

        return services;
    }
}

/// <summary>
/// Configurações de database
/// </summary>
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
}
