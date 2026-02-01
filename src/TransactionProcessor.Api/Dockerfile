# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/TransactionProcessor.Api/TransactionProcessor.Api.csproj", "src/TransactionProcessor.Api/"]
COPY ["src/TransactionProcessor.Application/TransactionProcessor.Application.csproj", "src/TransactionProcessor.Application/"]
COPY ["src/TransactionProcessor.Domain/TransactionProcessor.Domain.csproj", "src/TransactionProcessor.Domain/"]
COPY ["src/TransactionProcessor.Infrastructure/TransactionProcessor.Infrastructure.csproj", "src/TransactionProcessor.Infrastructure/"]
RUN dotnet restore "src/TransactionProcessor.Api/TransactionProcessor.Api.csproj"

COPY . .
RUN dotnet publish "src/TransactionProcessor.Api/TransactionProcessor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "TransactionProcessor.Api.dll"]
