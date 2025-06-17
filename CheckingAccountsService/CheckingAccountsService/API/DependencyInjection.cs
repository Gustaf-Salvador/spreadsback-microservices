using Amazon;
using Amazon.CognitoIdentityProvider;
using CheckingAccountsService.Application.Withdrawals.Commands;
using CheckingAccountsService.Domain.Repositories;
using CheckingAccountsService.Domain.Services;
using CheckingAccountsService.Infrastructure.Authentication;
using CheckingAccountsService.Infrastructure.Persistence;
using CheckingAccountsService.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CheckingAccountsService.API;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Settings
        var databaseSettings = new DatabaseSettings
        {
            ConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? 
                "Host=localhost;Port=5432;Database=checking_accounts;Username=postgres;Password=postgres"
        };

        var cognitoSettings = new CognitoSettings
        {
            UserPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? "us-east-1_example",
            AppClientId = Environment.GetEnvironmentVariable("COGNITO_APP_CLIENT_ID") ?? "example",
            Region = Environment.GetEnvironmentVariable("COGNITO_REGION") ?? "us-east-1"
        };

        services.AddSingleton(databaseSettings);
        services.AddSingleton(cognitoSettings);

        // AWS Services
        services.AddSingleton<IAmazonCognitoIdentityProvider>(_ => 
            new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(cognitoSettings.Region)));

        // Infrastructure Services
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ICognitoService, CognitoService>();

        // Repositories
        services.AddSingleton<ICheckingAccountRepository, CheckingAccountRepository>();
        services.AddSingleton<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IWithdrawalLimitRepository, WithdrawalLimitRepository>();

        // Domain Services
        services.AddSingleton<WithdrawalService>();

        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Register validators manually
        services.AddSingleton<IValidator<CreateWithdrawalCommand>, CreateWithdrawalCommandValidator>();

        return services;
    }
}