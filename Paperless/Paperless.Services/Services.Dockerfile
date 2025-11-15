# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
       ghostscript \
       libleptonica-dev \
       libtesseract-dev \
    && rm -rf /var/lib/apt/lists/*

ENV TESSDATA_PREFIX=/app

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
WORKDIR /app/x64
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
RUN ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so
WORKDIR /app

EXPOSE 8082

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy DAL project first (dependency)
COPY ["Paperless.REST/Paperless.DAL/Paperless.DAL.csproj", "Paperless.REST/Paperless.DAL/"]
# Copy Services project
COPY ["Paperless.Services/Paperless.Services.csproj", "Paperless.Services/"]
# Restore dependencies
RUN dotnet restore "Paperless.Services/Paperless.Services.csproj"
# Copy all source files
COPY Paperless.REST/Paperless.DAL/ Paperless.REST/Paperless.DAL/
COPY Paperless.Services/ Paperless.Services/
WORKDIR "/src/Paperless.Services"
RUN dotnet build "Paperless.Services.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/Paperless.Services"
RUN dotnet publish "Paperless.Services.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY Paperless.Services/tessdata /app/tessdata
ENTRYPOINT ["dotnet", "Paperless.Services.dll"]