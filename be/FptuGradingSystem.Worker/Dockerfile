FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FptuGradingSystem.Worker/FptuGradingSystem.Worker.csproj", "FptuGradingSystem.Worker/"]
COPY ["FptuGradingSystem.Infrastructure/FptuGradingSystem.Infrastructure.csproj", "FptuGradingSystem.Infrastructure/"]
COPY ["FptuGradingSystem.Application/FptuGradingSystem.Application.csproj", "FptuGradingSystem.Application/"]
COPY ["FptuGradingSystem.Domain/FptuGradingSystem.Domain.csproj", "FptuGradingSystem.Domain/"]
RUN dotnet restore "FptuGradingSystem.Worker/FptuGradingSystem.Worker.csproj"
COPY . .
WORKDIR "/src/FptuGradingSystem.Worker"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FptuGradingSystem.Worker.dll"]
