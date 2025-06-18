FROM public.ecr.aws/lambda/dotnet:8.0-arm64 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CheckingAccountsService.csproj", "./"]
RUN dotnet restore "CheckingAccountsService.csproj" --runtime linux-arm64

COPY . .
WORKDIR "/src"
RUN dotnet build "CheckingAccountsService.csproj" -c Release -o /app/build --no-restore --runtime linux-arm64

FROM build AS publish
RUN dotnet publish "CheckingAccountsService.csproj" \
    --runtime linux-arm64 \
    --self-contained false \
    -c Release \
    -o /app/publish \
    --no-restore

FROM base AS final
WORKDIR /var/task
COPY --from=publish /app/publish .

CMD ["CheckingAccountsService::CheckingAccountsService.ApiGatewayHandler::FunctionHandler"]