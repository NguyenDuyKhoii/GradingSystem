FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FptuGradingSystem.Gateway/FptuGradingSystem.Gateway.csproj", "FptuGradingSystem.Gateway/"]
RUN dotnet restore "FptuGradingSystem.Gateway/FptuGradingSystem.Gateway.csproj"
COPY . .
WORKDIR "/src/FptuGradingSystem.Gateway"
RUN dotnet publish "FptuGradingSystem.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FptuGradingSystem.Gateway.dll"]
