# SpreadsBack Common Services

Esta é uma biblioteca comum que fornece funcionalidades reutilizáveis para todos os microserviços do SpreadsBack.

## Funcionalidades

### 1. Modelos Base
- `ApiResponse<T>`: Resposta padronizada para APIs
- `PaginatedResult<T>`: Resultados paginados
- `PaginationParams`: Parâmetros de paginação

### 2. Handlers Base
- `BaseHandler<TRequest, TResponse>`: Handler base com validação e logging automático
- `ICommand<TResponse>` e `IQuery<TResponse>`: Interfaces para CQRS

### 3. Entidades Base
- `BaseEntity`: Entidade base com Id, CreatedAt, UpdatedAt
- `UserOwnedEntity`: Entidade com UserId
- `FinancialEntity`: Entidade para transações financeiras
- `ProcessedEvent` e `FailedEvent`: Para controle de eventos

### 4. Infraestrutura
- `BaseDbContext`: DbContext base com configurações comuns
- `BaseRepository<T>`: Repositório base com operações CRUD
- `BaseApiGatewayHandler`: Handler base para API Gateway
- `BaseSqsEventProcessor`: Processador base para eventos SQS

### 5. Utilitários
- `JwtUtils`: Utilitários para JWT
- `ValidationUtils`: Utilitários de validação

## Como usar

### 1. Instalar o pacote

```xml
<PackageReference Include="SpreadsBack.CommonServices" Version="1.0.0" />
```

### 2. Configurar no Startup

```csharp
using SpreadsBack.CommonServices.Infrastructure.Configuration;

// No seu Program.cs ou Startup.cs
services.AddCommonServices(Assembly.GetExecutingAssembly());
services.AddDbContext<YourDbContext>("connection_string");
```

### 3. Herdar do DbContext base

```csharp
public class YourDbContext : BaseDbContext
{
    public DbSet<YourEntity> YourEntities { get; set; }

    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options)
    {
    }

    protected override void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Configure suas entidades específicas aqui
        modelBuilder.Entity<YourEntity>(entity =>
        {
            entity.ToTable("your_table");
            // ... outras configurações
        });
    }
}
```

### 4. Criar Handlers

```csharp
public class GetYourDataQueryHandler : BaseHandler<GetYourDataQuery, YourDataDto>
{
    private readonly IYourService _yourService;

    public GetYourDataQueryHandler(
        ILogger<GetYourDataQueryHandler> logger,
        IValidator<GetYourDataQuery> validator,
        IYourService yourService) 
        : base(logger, validator)
    {
        _yourService = yourService;
    }

    protected override async Task<ApiResponse<YourDataDto>> ExecuteAsync(
        GetYourDataQuery request, 
        CancellationToken cancellationToken)
    {
        var data = await _yourService.GetDataAsync(request.Id);
        return ApiResponse<YourDataDto>.SuccessResponse(data);
    }
}
```

### 5. Criar API Gateway Handler

```csharp
public class YourApiGatewayHandler : BaseApiGatewayHandler
{
    public YourApiGatewayHandler(IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
    }

    protected override async Task<APIGatewayProxyResponse> RouteRequestAsync(
        APIGatewayProxyRequest request, 
        ILambdaContext context)
    {
        if (request.HttpMethod == "GET" && request.Path.StartsWith("/api/data"))
        {
            var query = new GetYourDataQuery { /* ... */ };
            var result = await Mediator.Send(query);
            return CreateApiResponse(result);
        }

        return CreateErrorResponse(HttpStatusCode.NotFound, "Route not found");
    }
}
```

### 6. Usar Repositórios

```csharp
public class YourService
{
    private readonly IBaseRepository<YourEntity> _repository;

    public YourService(IBaseRepository<YourEntity> repository)
    {
        _repository = repository;
    }

    public async Task<YourEntity?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<PaginatedResult<YourEntity>> GetPaginatedAsync(PaginationParams pagination)
    {
        return await _repository.GetPaginatedAsync(pagination);
    }
}
```

## Vantagens

- **Reutilização de código**: Funcionalidades comuns centralizadas
- **Padronização**: Todos os microserviços seguem os mesmos padrões
- **Manutenibilidade**: Mudanças em funcionalidades comuns são aplicadas automaticamente
- **Produtividade**: Desenvolvimento mais rápido de novos microserviços
- **Qualidade**: Código testado e reutilizado
- **Logging consistente**: Logging automático em todos os handlers
- **Validação automática**: Validação automática usando FluentValidation
- **Tratamento de erros**: Tratamento de erros padronizado

## Migração do CheckingAccountsService

Para migrar o CheckingAccountsService para usar esta biblioteca:

1. Remover classes duplicadas (ApiResponse, BaseEntity, etc.)
2. Alterar handlers para herdar de BaseHandler
3. Alterar DbContext para herdar de BaseDbContext
4. Usar os repositórios base
5. Atualizar o API Gateway handler

## Exemplo de migração completa

Veja a pasta `Examples/` para exemplos completos de como migrar um microserviço existente.
