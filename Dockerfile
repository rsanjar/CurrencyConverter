# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]

# Source project files
COPY ["src/CurrencyConverter.Api/CurrencyConverter.Api.csproj", "src/CurrencyConverter.Api/"]
COPY ["src/CurrencyConverter.Infrastructure/CurrencyConverter.Infrastructure.csproj", "src/CurrencyConverter.Infrastructure/"]
COPY ["src/CurrencyConverter.Application/CurrencyConverter.Application.csproj", "src/CurrencyConverter.Application/"]
COPY ["src/CurrencyConverter.Domain/CurrencyConverter.Domain.csproj", "src/CurrencyConverter.Domain/"]
COPY ["src/CurrencyConverter.Contracts/CurrencyConverter.Contracts.csproj", "src/CurrencyConverter.Contracts/"]

# Test project files — copied before the full COPY so NuGet restore is cached independently of source changes
COPY ["tests/CurrencyConverter.UnitTests/CurrencyConverter.UnitTests.csproj", "tests/CurrencyConverter.UnitTests/"]
COPY ["tests/CurrencyConverter.IntegrationTests/CurrencyConverter.IntegrationTests.csproj", "tests/CurrencyConverter.IntegrationTests/"]

RUN dotnet restore "./src/CurrencyConverter.Api/CurrencyConverter.Api.csproj"
RUN dotnet restore "./tests/CurrencyConverter.UnitTests/CurrencyConverter.UnitTests.csproj"
RUN dotnet restore "./tests/CurrencyConverter.IntegrationTests/CurrencyConverter.IntegrationTests.csproj"

COPY . .
WORKDIR "/src/src/CurrencyConverter.Api"
RUN dotnet build "./CurrencyConverter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build


# This stage runs all test suites. The Docker build fails here if any test fails,
# which blocks the publish and final stages — preventing a broken image from being deployed.
FROM build AS test
WORKDIR /src

RUN dotnet test "tests/CurrencyConverter.UnitTests/CurrencyConverter.UnitTests.csproj" \
    --no-restore \
    --configuration Release \
    --logger "trx;LogFileName=unit-tests.trx" \
    --results-directory /app/test-results \
    --verbosity normal

RUN dotnet test "tests/CurrencyConverter.IntegrationTests/CurrencyConverter.IntegrationTests.csproj" \
    --no-restore \
    --configuration Release \
    --logger "trx;LogFileName=integration-tests.trx" \
    --results-directory /app/test-results \
    --verbosity normal


# This stage is used to publish the service project to be copied to the final stage.
# It starts FROM test, so publish is only reached when all tests pass.
FROM test AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/CurrencyConverter.Api"
RUN dotnet publish "./CurrencyConverter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CurrencyConverter.Api.dll"]
