# ─── Stage 1: restore & publish ───────────────────────────────────────────────
# Build context is the SOLUTION ROOT (set in docker-compose.yml via context: .)
# so paths here are relative to the solution root.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first so the restore layer is cached independently.
# Re-runs only when the .csproj changes, not on every source change.
COPY NINJA.RabbitMQ.Producer.API/NINJA.RabbitMQ.Producer.API.csproj \
     NINJA.RabbitMQ.Producer.API/

RUN dotnet restore NINJA.RabbitMQ.Producer.API/NINJA.RabbitMQ.Producer.API.csproj

# Copy the rest of the project source and publish in Release mode.
COPY NINJA.RabbitMQ.Producer.API/ NINJA.RabbitMQ.Producer.API/

RUN dotnet publish NINJA.RabbitMQ.Producer.API/NINJA.RabbitMQ.Producer.API.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# ─── Stage 2: runtime image ───────────────────────────────────────────────────
# aspnet:8.0 includes the ASP.NET Core runtime (~100 MB smaller than sdk:8.0).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as a non-root user — defence-in-depth for production containers.
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

# Kestrel listens on HTTP only inside Docker; TLS is terminated at the
# load-balancer / reverse-proxy layer in production.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "NINJA.RabbitMQ.Producer.API.dll"]
