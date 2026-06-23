FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AITrainingSystem.API/AITrainingSystem.API.csproj", "AITrainingSystem.API/"]
COPY ["AITrainingSystem.Application/AITrainingSystem.Application.csproj", "AITrainingSystem.Application/"]
COPY ["AITrainingSystem.Domain/AITrainingSystem.Domain.csproj", "AITrainingSystem.Domain/"]
COPY ["AITrainingSystem.Infrastructure/AITrainingSystem.Infrastructure.csproj", "AITrainingSystem.Infrastructure/"]
COPY ["AITrainingSystem.Persistence/AITrainingSystem.Persistence.csproj", "AITrainingSystem.Persistence/"]
RUN dotnet restore "AITrainingSystem.API/AITrainingSystem.API.csproj"
COPY . .
WORKDIR "/src/AITrainingSystem.API"
RUN dotnet build "AITrainingSystem.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AITrainingSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AITrainingSystem.API.dll"]
