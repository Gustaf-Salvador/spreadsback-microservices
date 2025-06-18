# 🚀 CommonServices Implementation Checklist

Use este checklist para migrar qualquer microserviço para usar a biblioteca `SpreadsBack.CommonServices`.

## ✅ Fase 1: Preparação (15-30 min)

### 1.1 Instalação
- [ ] Instalar o pacote: `dotnet add package SpreadsBack.CommonServices`
- [ ] Verificar se o build funciona após instalação
- [ ] Backup do código atual (commit Git)

### 1.2 Análise do Código Existente
- [ ] Identificar handlers existentes (`IRequestHandler` implementations)
- [ ] Identificar entidades do domínio
- [ ] Identificar repositórios
- [ ] Identificar DTOs
- [ ] Identificar Lambda functions (API Gateway, SQS)
- [ ] Identificar código de autenticação/JWT
- [ ] Identificar código de logging

## ✅ Fase 2: Configuração Base (30-45 min)

### 2.1 Program.cs/Startup.cs
- [ ] Adicionar `using SpreadsBack.CommonServices.Infrastructure.Configuration;`
- [ ] Substituir configurações manuais por `builder.Services.AddCommonServices(Assembly.GetExecutingAssembly());`
- [ ] Configurar DbContext usando `AddDatabaseSettings()`
- [ ] Configurar AWS services com `AddAwsServices()`

### 2.2 Packages Cleanup
- [ ] Remover dependências duplicadas do .csproj (MediatR, FluentValidation se já incluídas na CommonServices)
- [ ] Manter apenas dependências específicas do domínio

## ✅ Fase 3: Migração de Entidades (30-60 min)

### 3.1 Domain Entities
- [ ] **BaseEntity**: Entidades simples → herdar de `BaseEntity`
- [ ] **UserOwnedEntity**: Entidades com UserId → herdar de `UserOwnedEntity`
- [ ] **FinancialEntity**: Entidades financeiras → herdar de `FinancialEntity`
- [ ] Remover propriedades duplicadas (Id, CreatedAt, UpdatedAt, UserId)
- [ ] Testar build após cada migração

### 3.2 DbContext
- [ ] Herdar de `BaseDbContext` em vez de `DbContext`
- [ ] Mover configurações específicas para `ConfigureEntities()`
- [ ] Remover configurações de `ProcessedEvent` e `FailedEvent` (já na base)
- [ ] Testar conexão com banco

## ✅ Fase 4: Migração de DTOs (20-30 min)

### 4.1 Response DTOs
- [ ] DTOs simples → herdar de `BaseDto`
- [ ] DTOs com UserId → herdar de `BaseUserOwnedDto`
- [ ] Listas paginadas → herdar de `BasePaginatedDto<T>`

### 4.2 Request DTOs
- [ ] Create operations → herdar de `BaseCreateDto`
- [ ] Update operations → herdar de `BaseUpdateDto`
- [ ] Remover propriedades duplicadas

## ✅ Fase 5: Migração de Handlers (60-90 min)

### 5.1 Preparação
- [ ] Queries → implementar `IQuery<TResponse>`
- [ ] Commands → implementar `ICommand<TResponse>`

### 5.2 Handler Migration
Para cada handler existente:
- [ ] Herdar de `BaseHandler<TRequest, TResponse>`
- [ ] Mover lógica principal para `ExecuteAsync()`
- [ ] Remover código manual de validação e logging
- [ ] Remover try/catch manual (já tratado na base)
- [ ] Adicionar event tracking: `await _eventTracker.TrackEventAsync()`
- [ ] Adicionar SNS publishing se necessário: `await _eventPublisher.PublishEventAsync()`

### 5.3 Validação
- [ ] Verificar se validators FluentValidation estão registrados
- [ ] Testar validação automática

## ✅ Fase 6: Migração de Repositórios (30-45 min)

### 6.1 Interfaces
- [ ] Interfaces → herdar de `IBaseRepository<TEntity>`
- [ ] Manter apenas métodos específicos do domínio

### 6.2 Implementações
- [ ] Repositórios → herdar de `BaseRepository<TEntity>`
- [ ] Remover implementações CRUD básicas
- [ ] Manter apenas métodos de negócio específicos

## ✅ Fase 7: Migração de Lambda Functions (45-60 min)

### 7.1 API Gateway Functions
- [ ] Handler → herdar de `BaseApiGatewayHandler`
- [ ] Implementar `RouteRequestAsync()`
- [ ] Implementar `ValidateAuthenticationAsync()` usando `JwtUtils` ou `CognitoService`
- [ ] Remover código manual de roteamento e autenticação

### 7.2 SQS Functions
- [ ] Processor → herdar de `BaseSqsMessageProcessor<TMessage>`
- [ ] Implementar `ProcessMessageAsync()`
- [ ] Remover tratamento manual de erro e logging

### 7.3 Function.cs
- [ ] Configurar DI container no construtor
- [ ] Usar handlers da CommonServices

## ✅ Fase 8: Migração de Utilitários (15-30 min)

### 8.1 JWT/Authentication
- [ ] Substituir código manual por `JwtUtils.ExtractUserIdFromToken()`
- [ ] Usar `CognitoService` para validação de usuários

### 8.2 Validation
- [ ] Substituir validações manuais por `ValidationUtils`
- [ ] `IsValidEmail()`, `IsValidGuid()`, `SanitizeString()`

### 8.3 Logging
- [ ] Remover configuração manual de logging
- [ ] Usar `LambdaLoggerAdapter` para Lambda functions

## ✅ Fase 9: Cleanup e Testes (30-45 min)

### 9.1 Código Duplicado
- [ ] Remover classes antigas que foram substituídas
- [ ] Remover imports não utilizados
- [ ] Limpar arquivos de configuração antigos

### 9.2 Testes
- [ ] Atualizar testes unitários para novos handlers
- [ ] Testar todos os endpoints
- [ ] Verificar logs
- [ ] Testar event tracking
- [ ] Validar autenticação

### 9.3 Build Final
- [ ] `dotnet build` sem erros
- [ ] `dotnet test` todos os testes passando
- [ ] Deploy em ambiente de desenvolvimento
- [ ] Teste de smoke em prod

## ✅ Fase 10: Documentação (15 min)

- [ ] Atualizar README do microserviço
- [ ] Documentar mudanças específicas
- [ ] Commit e PR com descrição das alterações

## 📊 Tempo Estimado Total

- **Microserviço Simples** (1-2 entities, 3-5 handlers): **4-6 horas**
- **Microserviço Médio** (3-5 entities, 6-10 handlers): **6-8 horas**
- **Microserviço Complexo** (5+ entities, 10+ handlers): **8-12 horas**

## 🚨 Pontos de Atenção

1. **Database Migrations**: Verifique se as mudanças de entidades necessitam migrations
2. **Breaking Changes**: Cuidado com mudanças em contratos de API
3. **Performance**: Valide se a performance não foi impactada
4. **Dependencies**: Alguns packages podem ser removidos após migração
5. **Environment Variables**: Configurações AWS podem precisar de ajustes

## 🎯 Critérios de Sucesso

- ✅ Build sem erros
- ✅ Todos os testes passando
- ✅ Funcionalidade preservada
- ✅ Logs consistentes
- ✅ Performance mantida ou melhorada
- ✅ Código mais limpo e padronizado

## 📞 Suporte

- Consultar **MIGRATION_GUIDE.md** para exemplos detalhados
- Verificar **README.md** da CommonServices
- Em caso de dúvidas, revisar implementação no CheckingAccountsService

---

**💡 Dica**: Faça a migração incrementalmente, testando após cada fase para identificar problemas rapidamente.
