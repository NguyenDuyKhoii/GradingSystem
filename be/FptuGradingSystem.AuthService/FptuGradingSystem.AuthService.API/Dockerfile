FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.API/FptuGradingSystem.AuthService.API.csproj", "FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.API/"]
COPY ["FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.Infrastructure/FptuGradingSystem.AuthService.Infrastructure.csproj", "FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.Infrastructure/"]
COPY ["FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.Application/FptuGradingSystem.AuthService.Application.csproj", "FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.Application/"]
COPY ["FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.Domain/FptuGradingSystem.AuthService.Domain.csproj", "FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.Domain/"]
RUN dotnet restore "FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.API/FptuGradingSystem.AuthService.API.csproj"
COPY . .
WORKDIR "/src/FptuGradingSystem.AuthService/FptuGradingSystem.AuthService.API"
RUN dotnet publish "FptuGradingSystem.AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FptuGradingSystem.AuthService.API.dll"]
