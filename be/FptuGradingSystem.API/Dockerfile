FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FptuGradingSystem.API/FptuGradingSystem.API.csproj", "FptuGradingSystem.API/"]
COPY ["FptuGradingSystem.Infrastructure/FptuGradingSystem.Infrastructure.csproj", "FptuGradingSystem.Infrastructure/"]
COPY ["FptuGradingSystem.Application/FptuGradingSystem.Application.csproj", "FptuGradingSystem.Application/"]
COPY ["FptuGradingSystem.Domain/FptuGradingSystem.Domain.csproj", "FptuGradingSystem.Domain/"]
COPY ["FptuGradingSystem.GrpcService/Protos/", "FptuGradingSystem.GrpcService/Protos/"]
RUN dotnet restore "FptuGradingSystem.API/FptuGradingSystem.API.csproj"
COPY . .
WORKDIR "/src/FptuGradingSystem.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FptuGradingSystem.API.dll"]
