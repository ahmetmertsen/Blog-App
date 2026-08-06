# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# csproj'lar once kopyalanir ki kaynak degistiginde restore katmani cache'ten gelsin.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Core/buduns-server.Domain/*.csproj src/Core/buduns-server.Domain/
COPY src/Core/buduns-server.Application/*.csproj src/Core/buduns-server.Application/
COPY src/Infrastructure/buduns-server.Infrastructure/*.csproj src/Infrastructure/buduns-server.Infrastructure/
COPY src/Infrastructure/buduns-server.Persistence/*.csproj src/Infrastructure/buduns-server.Persistence/
COPY src/Presentation/buduns-server.WebAPI/*.csproj src/Presentation/buduns-server.WebAPI/
RUN dotnet restore src/Presentation/buduns-server.WebAPI/buduns-server.WebAPI.csproj

COPY src/ src/
RUN dotnet publish src/Presentation/buduns-server.WebAPI/buduns-server.WebAPI.csproj \
    -c Release -o /app --no-restore

FROM build AS migrator
RUN dotnet tool install --global dotnet-ef --version 8.0.29
ENV PATH="/root/.dotnet/tools:${PATH}"
ENTRYPOINT ["dotnet", "ef", "database", "update", \
    "--project", "src/Infrastructure/buduns-server.Persistence", \
    "--startup-project", "src/Presentation/buduns-server.WebAPI"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "buduns-server.WebAPI.dll"]
