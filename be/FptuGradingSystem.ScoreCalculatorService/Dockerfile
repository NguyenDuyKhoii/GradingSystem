FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FptuGradingSystem.ScoreCalculatorService/FptuGradingSystem.ScoreCalculatorService.csproj", "FptuGradingSystem.ScoreCalculatorService/"]
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.Infrastructure/FptuGradingSystem.Infrastructure.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.Infrastructure/"]
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.Application/FptuGradingSystem.Application.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.Application/"]
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.Domain/FptuGradingSystem.Domain.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.Domain/"]
RUN dotnet restore "FptuGradingSystem.ScoreCalculatorService/FptuGradingSystem.ScoreCalculatorService.csproj"
COPY . .
WORKDIR "/src/FptuGradingSystem.ScoreCalculatorService"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FptuGradingSystem.ScoreCalculatorService.dll"]
