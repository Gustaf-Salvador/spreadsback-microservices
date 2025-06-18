# Guia de Migração para SpreadsBack.CommonServices

Este guia mostra como migrar o CheckingAccountsService (e outros microserviços) para usar a biblioteca comum SpreadsBack.CommonServices.

## 1. Instalar o Pacote

Adicione a referência ao pacote no seu `.csproj`:

```xml
<PackageReference Include="SpreadsBack.CommonServices" Version="1.0.0" />
```

Ou usando o comando dotnet:
```bash
dotnet add package SpreadsBack.CommonServices
```

## 2. Atualizar o Program.cs / Startup.cs

```csharp
using SpreadsBack.CommonServices.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços comuns
builder.Services.AddCommonServices(Assembly.GetExecutingAssembly());

// Configurar DbContext
builder.Services.AddDbContext<CheckingAccountDbContext>(connectionString);

// Configurar database settings
builder.Services.AddDatabaseSettings(settings =>
{
    settings.ConnectionString = connectionString;
    settings.EnableDetailedErrors = false;
    settings.CommandTimeout = 30;
});

var app = builder.Build();
```

## 3. Migrar Entidades

### Antes:
```csharp
public class CheckingAccount
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string CurrencyId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Domain.Entities;

public class CheckingAccount : UserOwnedEntity
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
```

## 4. Migrar DbContext

### Antes:
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<CheckingAccount> CheckingAccounts { get; set; }
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }
    public DbSet<FailedEvent> FailedEvents { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurações manuais para ProcessedEvent, FailedEvent, etc.
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Persistence;

public class CheckingAccountDbContext : BaseDbContext
{
    public DbSet<CheckingAccount> CheckingAccounts { get; set; } = null!;

    public CheckingAccountDbContext(DbContextOptions<CheckingAccountDbContext> options) 
        : base(options)
    {
    }

    protected override void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Apenas configurações específicas do CheckingAccount
        modelBuilder.Entity<CheckingAccount>(entity =>
        {
            entity.ToTable("checking_accounts");
            entity.HasIndex(e => new { e.UserId, e.CurrencyId }).IsUnique();
        });
    }
}
```

## 5. Migrar Handlers

### Antes:
```csharp
public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, ApiResponse<BalanceDto>>
{
    private readonly IValidator<GetBalanceQuery> _validator;
    private readonly ILogger<GetBalanceQueryHandler> _logger;
    private readonly ICheckingAccountRepository _repository;

    public async Task<ApiResponse<BalanceDto>> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ApiResponse<BalanceDto>.ErrorResponse("Validation failed", errors);
            }

            // Lógica do handler...
            var account = await _repository.GetByUserAndCurrencyAsync(request.UserId, request.CurrencyId);
            
            if (account == null)
            {
                return ApiResponse<BalanceDto>.ErrorResponse("Account not found");
            }

            var balanceDto = new BalanceDto
            {
                UserId = account.UserId,
                CurrencyId = account.CurrencyId,
                Balance = account.Balance,
                LastUpdated = account.UpdatedAt
            };

            return ApiResponse<BalanceDto>.SuccessResponse(balanceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetBalanceQuery");
            return ApiResponse<BalanceDto>.ErrorResponse("An error occurred");
        }
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Application.Handlers;
using SpreadsBack.CommonServices.Application.Queries;

public class GetBalanceQuery : IQuery<BalanceDto>
{
    public string UserId { get; set; } = string.Empty;
    public string CurrencyId { get; set; } = string.Empty;
}

public class GetBalanceQueryHandler : BaseHandler<GetBalanceQuery, BalanceDto>
{
    private readonly ICheckingAccountRepository _repository;

    public GetBalanceQueryHandler(
        ILogger<GetBalanceQueryHandler> logger,
        IValidator<GetBalanceQuery> validator,
        ICheckingAccountRepository repository)
        : base(logger, validator)
    {
        _repository = repository;
    }

    protected override async Task<ApiResponse<BalanceDto>> ExecuteAsync(
        GetBalanceQuery request, 
        CancellationToken cancellationToken)
    {
        var account = await _repository.GetByUserAndCurrencyAsync(request.UserId, request.CurrencyId);
        
        if (account == null)
        {
            return ApiResponse<BalanceDto>.NotFoundResponse("Account not found");
        }

        var balanceDto = new BalanceDto
        {
            UserId = account.UserId,
            CurrencyId = account.CurrencyId,
            Balance = account.Balance,
            LastUpdated = account.UpdatedAt
        };

        return ApiResponse<BalanceDto>.SuccessResponse(balanceDto);
    }
}
```

## 6. Migrar API Gateway Handler

### Antes:
```csharp
public class ApiGatewayHandler
{
    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Roteamento manual, tratamento de erros manual, autenticação manual...
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Lambda;
using SpreadsBack.CommonServices.Core.Utils;

public class CheckingAccountApiGatewayHandler : BaseApiGatewayHandler
{
    public CheckingAccountApiGatewayHandler(IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
    }

    protected override async Task<APIGatewayProxyResponse> RouteRequestAsync(
        APIGatewayProxyRequest request, 
        ILambdaContext context)
    {
        var pathParts = request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // GET /users/{userId}/balances/{currencyId}
        if (request.HttpMethod == "GET" && 
            pathParts.Length >= 4 && 
            pathParts[0] == "users" && 
            pathParts[2] == "balances")
        {
            var query = new GetBalanceQuery
            {
                UserId = pathParts[1],
                CurrencyId = pathParts[3]
            };
            
            var result = await Mediator.Send(query);
            return CreateApiResponse(result);
        }

        return CreateErrorResponse(HttpStatusCode.NotFound, "Route not found");
    }

    protected override async Task<AuthResult> ValidateAuthenticationAsync(APIGatewayProxyRequest request)
    {
        var authHeader = request.Headers?.GetValueOrDefault("Authorization");
        var userId = JwtUtils.ExtractUserIdFromToken(authHeader);
        
        if (string.IsNullOrEmpty(userId))
        {
            return AuthResult.Failure("Invalid or missing authentication token");
        }

        return AuthResult.Success(userId);
    }
}
```

## 7. Usar Repositórios Base

### Antes:
```csharp
public interface ICheckingAccountRepository
{
    Task<CheckingAccount?> GetByIdAsync(Guid id);
    Task<CheckingAccount> AddAsync(CheckingAccount entity);
    Task<CheckingAccount> UpdateAsync(CheckingAccount entity);
    Task DeleteAsync(Guid id);
    Task<CheckingAccount?> GetByUserAndCurrencyAsync(string userId, string currencyId);
}

public class CheckingAccountRepository : ICheckingAccountRepository
{
    private readonly DbContext _context;
    private readonly DbSet<CheckingAccount> _dbSet;

    public CheckingAccountRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<CheckingAccount>();
    }

    public async Task<CheckingAccount?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    // Implementação manual de todos os métodos CRUD...
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Repositories;

public interface ICheckingAccountRepository : IBaseRepository<CheckingAccount>
{
    Task<CheckingAccount?> GetByUserAndCurrencyAsync(string userId, string currencyId);
}

public class CheckingAccountRepository : BaseRepository<CheckingAccount>, ICheckingAccountRepository
{
    public CheckingAccountRepository(DbContext context) : base(context)
    {
    }

    public async Task<CheckingAccount?> GetByUserAndCurrencyAsync(string userId, string currencyId)
    {
        return await DbSet
            .FirstOrDefaultAsync(ca => ca.UserId == userId && ca.CurrencyId == currencyId);
    }
}
```

## 8. Atualizar Function.cs

### Antes:
```csharp
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CheckingAccountsService;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
    {
        var handler = new ApiGatewayHandler();
        return await handler.FunctionHandler(input, context);
    }
}
```

### Depois:
```csharp
using Microsoft.Extensions.DependencyInjection;
using SpreadsBack.CommonServices.Infrastructure.Configuration;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CheckingAccountsService;

public class Function
{
    private readonly IServiceProvider _serviceProvider;

    public Function()
    {
        var services = new ServiceCollection();
        
        // Configurar serviços
        services.AddCommonServices(Assembly.GetExecutingAssembly());
        services.AddDbContext<CheckingAccountDbContext>(connectionString);
        services.AddScoped<ICheckingAccountRepository, CheckingAccountRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
    {
        var handler = new CheckingAccountApiGatewayHandler(_serviceProvider);
        return await handler.FunctionHandler(input, context);
    }
}
```

## 9. Migrar Logging

### Antes:
```csharp
public class CheckingAccountHandler
{
    private readonly ILogger<CheckingAccountHandler> _logger;

    public async Task Handle()
    {
        _logger.LogInformation("Processing request...");
        // Lambda context logging manual
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Logging;

public class CheckingAccountHandler : BaseHandler<Query, Response>
{
    public CheckingAccountHandler(
        ILogger<CheckingAccountHandler> logger,
        IValidator<Query> validator)
        : base(logger, validator)
    {
        // Logging automático via BaseHandler
        // Lambda context logging já configurado via ServiceCollectionExtensions
    }
}
```

## 10. Migrar Event Tracking

### Antes:
```csharp
public class ManualEventTracker
{
    public async Task TrackEventAsync(string eventType, object data)
    {
        // Implementação manual de tracking
        var processedEvent = new ProcessedEvent
        {
            EventType = eventType,
            EventData = JsonSerializer.Serialize(data),
            ProcessedAt = DateTime.UtcNow
        };
        
        _context.ProcessedEvents.Add(processedEvent);
        await _context.SaveChangesAsync();
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.EventTracking;

public class CheckingAccountHandler : BaseHandler<CreateAccountCommand, AccountDto>
{
    private readonly IEfEventTracker _eventTracker;

    public CheckingAccountHandler(
        ILogger<CheckingAccountHandler> logger,
        IValidator<CreateAccountCommand> validator,
        IEfEventTracker eventTracker)
        : base(logger, validator)
    {
        _eventTracker = eventTracker;
    }

    protected override async Task<ApiResponse<AccountDto>> ExecuteAsync(
        CreateAccountCommand request, 
        CancellationToken cancellationToken)
    {
        // Lógica do handler...
        var account = await CreateAccount(request);

        // Event tracking automático
        await _eventTracker.TrackEventAsync(
            "AccountCreated", 
            new { AccountId = account.Id, UserId = account.UserId }, 
            account.UserId);

        return ApiResponse<AccountDto>.SuccessResponse(account);
    }
}
```

## 11. Migrar SNS Event Publishing

### Antes:
```csharp
public class ManualSnsPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;

    public async Task PublishEventAsync(string topicArn, object eventData)
    {
        var message = JsonSerializer.Serialize(eventData);
        await _snsClient.PublishAsync(topicArn, message);
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Messaging;

public class CheckingAccountHandler : BaseHandler<TransferCommand, TransferDto>
{
    private readonly ISnsEventPublisher _eventPublisher;

    public CheckingAccountHandler(
        ILogger<CheckingAccountHandler> logger,
        IValidator<TransferCommand> validator,
        ISnsEventPublisher eventPublisher)
        : base(logger, validator)
    {
        _eventPublisher = eventPublisher;
    }

    protected override async Task<ApiResponse<TransferDto>> ExecuteAsync(
        TransferCommand request, 
        CancellationToken cancellationToken)
    {
        // Lógica da transferência...
        var transfer = await ProcessTransfer(request);

        // Publicar evento de transferência
        await _eventPublisher.PublishEventAsync(
            "transfer-events", 
            "TransferCompleted",
            new 
            { 
                TransferId = transfer.Id,
                FromUserId = request.FromUserId,
                ToUserId = request.ToUserId,
                Amount = request.Amount,
                CurrencyId = request.CurrencyId
            });

        return ApiResponse<TransferDto>.SuccessResponse(transfer);
    }
}
```

## 12. Migrar SQS Message Processing

### Antes:
```csharp
public class Function
{
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        foreach (var record in sqsEvent.Records)
        {
            try
            {
                // Processamento manual de cada mensagem
                var message = JsonSerializer.Deserialize<TransferMessage>(record.Body);
                await ProcessTransferMessage(message);
            }
            catch (Exception ex)
            {
                // Tratamento manual de erro
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Lambda;

public class TransferSqsProcessor : BaseSqsMessageProcessor<TransferMessage>
{
    private readonly IMediator _mediator;

    public TransferSqsProcessor(
        IServiceProvider serviceProvider,
        IMediator mediator) 
        : base(serviceProvider)
    {
        _mediator = mediator;
    }

    protected override async Task ProcessMessageAsync(
        TransferMessage message, 
        SQSEvent.SQSMessage sqsMessage, 
        ILambdaContext context)
    {
        var command = new ProcessTransferCommand
        {
            TransferId = message.TransferId,
            FromUserId = message.FromUserId,
            ToUserId = message.ToUserId,
            Amount = message.Amount
        };

        await _mediator.Send(command);
    }
}

public class Function
{
    private readonly IServiceProvider _serviceProvider;

    public Function()
    {
        var services = new ServiceCollection();
        services.AddCommonServices(Assembly.GetExecutingAssembly());
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var processor = new TransferSqsProcessor(_serviceProvider, 
            _serviceProvider.GetRequiredService<IMediator>());
        await processor.ProcessEventAsync(sqsEvent, context);
    }
}
```

## 13. Migrar DTOs

### Antes:
```csharp
public class BalanceDto
{
    public string UserId { get; set; }
    public string CurrencyId { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CreateAccountDto
{
    public string UserId { get; set; }
    public string CurrencyId { get; set; }
    public decimal InitialBalance { get; set; }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Core.DTOs;

public class BalanceDto : BaseUserOwnedDto
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CreateAccountDto : BaseCreateDto
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
}

public class AccountListDto : BasePaginatedDto<AccountSummaryDto>
{
    // Herda automaticamente Items, TotalCount, Page, PageSize, TotalPages
}
```

## 14. Migrar Autenticação Cognito

### Antes:
```csharp
public class ManualCognitoAuth
{
    public async Task<string> GetUserIdFromTokenAsync(string token)
    {
        // Implementação manual de validação JWT
        // Lógica de verificação de token Cognito
    }
}
```

### Depois:
```csharp
using SpreadsBack.CommonServices.Infrastructure.Authentication;

public class CheckingAccountHandler : BaseHandler<GetAccountsQuery, List<AccountDto>>
{
    private readonly ICognitoService _cognitoService;

    public CheckingAccountHandler(
        ILogger<CheckingAccountHandler> logger,
        IValidator<GetAccountsQuery> validator,
        ICognitoService cognitoService)
        : base(logger, validator)
    {
        _cognitoService = cognitoService;
    }

    protected override async Task<ApiResponse<List<AccountDto>>> ExecuteAsync(
        GetAccountsQuery request, 
        CancellationToken cancellationToken)
    {
        // Validação automática de usuário via Cognito
        var userInfo = await _cognitoService.GetUserInfoAsync(request.UserId);
        
        if (userInfo == null)
        {
            return ApiResponse<List<AccountDto>>.UnauthorizedResponse("User not found");
        }

        // Lógica do handler...
        return ApiResponse<List<AccountDto>>.SuccessResponse(accounts);
    }
}
```

## 15. Usar Utilitários

### JWT Utils:
```csharp
using SpreadsBack.CommonServices.Core.Utils;

// Extrair UserId do token JWT
var userId = JwtUtils.ExtractUserIdFromToken(authorizationHeader);

// Validar token
var isValid = JwtUtils.IsTokenValid(token, issuer, audience);
```

### Validation Utils:
```csharp
using SpreadsBack.CommonServices.Core.Utils;

// Validar email
if (!ValidationUtils.IsValidEmail(email))
{
    return ApiResponse<UserDto>.ValidationErrorResponse("Invalid email format");
}

// Validar GUID
if (!ValidationUtils.IsValidGuid(accountId))
{
    return ApiResponse<AccountDto>.ValidationErrorResponse("Invalid account ID format");
}

// Sanitizar string
var cleanInput = ValidationUtils.SanitizeString(userInput);
```

## 16. Benefícios da Migração

### ✅ Antes da Migração
- Código duplicado em vários microserviços
- Tratamento de erros inconsistente
- Logging manual e inconsistente
- Validação manual repetitiva
- Configuração de DbContext repetitiva
- Event tracking manual e inconsistente
- SNS publishing manual
- SQS processing com tratamento de erro manual
- Autenticação Cognito repetitiva
- DTOs sem padronização

### ✅ Depois da Migração
- **Reutilização de código**: Funcionalidades comuns centralizadas
- **Padronização**: Todos os microserviços seguem os mesmos padrões
- **Logging automático**: Logging consistente em todos os handlers
- **Validação automática**: Validação usando FluentValidation
- **Tratamento de erros padronizado**: Respostas de erro consistentes
- **Event tracking centralizado**: EfEventTracker para auditoria
- **Messaging padronizado**: SNS publishing e SQS processing automáticos
- **Autenticação centralizada**: CognitoService para validação de usuários
- **DTOs base**: Estruturas comuns para requests/responses
- **Utilitários**: JWT e validation helpers prontos para uso
- **Produtividade**: Desenvolvimento mais rápido de novos microserviços
- **Manutenibilidade**: Mudanças em funcionalidades comuns aplicadas automaticamente

## 10. Checklist de Migração

- [ ] Instalar pacote SpreadsBack.CommonServices
- [ ] Migrar entidades para herdar de BaseEntity/UserOwnedEntity
- [ ] Migrar DbContext para herdar de BaseDbContext
- [ ] Migrar handlers para herdar de BaseHandler
- [ ] Migrar queries/commands para usar interfaces base
- [ ] Migrar repositórios para herdar de BaseRepository
- [ ] Migrar API Gateway handler para herdar de BaseApiGatewayHandler
- [ ] Atualizar Function.cs para usar DI
- [ ] Atualizar testes unitários
- [ ] Remover código duplicado
- [ ] Testar todos os endpoints
- [ ] Verificar logs e métricas

## Suporte

Para dúvidas ou problemas na migração, consulte:
- README.md da biblioteca CommonServices
- Exemplos na pasta Examples/
- Documentação técnica do projeto
