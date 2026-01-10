# Stage 1: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only csproj files first (for Caching)
COPY Paperless.REST/Paperless.DAL/Paperless.DAL.csproj Paperless.DAL/
COPY Paperless.REST/Paperless.BL/Paperless.BL.csproj Paperless.BL/
COPY Paperless.REST/Paperless.API/Paperless.API.csproj Paperless.API/

# Restore dependencies
RUN dotnet restore Paperless.API/Paperless.API.csproj

# Copy all source code
COPY Paperless.REST/Paperless.DAL/ Paperless.DAL
COPY Paperless.REST/Paperless.BL/ Paperless.BL
COPY Paperless.REST/Paperless.API/ Paperless.API

# Build & Publish
WORKDIR /src/Paperless.API
RUN dotnet publish -c Release -o /publish /p:UseAppHost=false

# Stage 2: Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Paperless.API.dll"]