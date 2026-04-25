# ─── Stage 1: restore & publish ───────────────────────────────────────────────
# Build context is the SOLUTION ROOT (set in docker-compose.yml via context: .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NINJA.RabbitMQ.Subscriber/NINJA.RabbitMQ.Subscriber.csproj \
     NINJA.RabbitMQ.Subscriber/

RUN dotnet restore NINJA.RabbitMQ.Subscriber/NINJA.RabbitMQ.Subscriber.csproj

COPY NINJA.RabbitMQ.Subscriber/ NINJA.RabbitMQ.Subscriber/

RUN dotnet publish NINJA.RabbitMQ.Subscriber/NINJA.RabbitMQ.Subscriber.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# ─── Stage 2: runtime image ───────────────────────────────────────────────────
# runtime:8.0 (not aspnet:8.0) — the Subscriber has no HTTP server, so the
# smaller runtime image is sufficient (~50 MB less than aspnet).
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NINJA.RabbitMQ.Subscriber.dll"]
