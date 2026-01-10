# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
EXPOSE 8083

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Project file for caching
COPY ["Paperless.Batch/Paperless.Batch.csproj", "."]

# Restore dependencies
RUN dotnet restore "./Paperless.Batch.csproj"

# Copy source code
COPY Paperless.Batch/ .

# Build project
RUN dotnet build "./Paperless.Batch.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Paperless.Batch.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app

# Copy published DLLs
COPY --from=publish /app/publish .

# Start Batch Application
ENTRYPOINT ["dotnet", "Paperless.Batch.dll"]