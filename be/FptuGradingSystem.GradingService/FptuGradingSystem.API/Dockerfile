FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.API/FptuGradingSystem.API.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.API/"]
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.Infrastructure/FptuGradingSystem.Infrastructure.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.Infrastructure/"]
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.Application/FptuGradingSystem.Application.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.Application/"]
COPY ["FptuGradingSystem.GradingService/FptuGradingSystem.Domain/FptuGradingSystem.Domain.csproj", "FptuGradingSystem.GradingService/FptuGradingSystem.Domain/"]
COPY ["FptuGradingSystem.ScoreCalculatorService/Protos/", "FptuGradingSystem.ScoreCalculatorService/Protos/"]
RUN dotnet restore "FptuGradingSystem.GradingService/FptuGradingSystem.API/FptuGradingSystem.API.csproj"
COPY . .
WORKDIR "/src/FptuGradingSystem.GradingService/FptuGradingSystem.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FptuGradingSystem.API.dll"]
