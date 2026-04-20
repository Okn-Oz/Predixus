FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Predixus.API/Predixus.API.csproj", "src/Predixus.API/"]
COPY ["src/Predixus.Application/Predixus.Application.csproj", "src/Predixus.Application/"]
COPY ["src/Predixus.Domain/Predixus.Domain.csproj", "src/Predixus.Domain/"]
COPY ["src/Predixus.Infrastructure/Predixus.Infrastructure.csproj", "src/Predixus.Infrastructure/"]
RUN dotnet restore "src/Predixus.API/Predixus.API.csproj"
COPY . .
RUN dotnet publish "src/Predixus.API/Predixus.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Predixus.API.dll"]
