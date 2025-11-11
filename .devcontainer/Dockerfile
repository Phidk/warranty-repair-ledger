FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends sqlite3 \
    && rm -rf /var/lib/apt/lists/*
