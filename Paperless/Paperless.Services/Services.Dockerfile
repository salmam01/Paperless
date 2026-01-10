# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base
WORKDIR /app

# Install Tesseract and necessary dependencies
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
       ghostscript \
       libleptonica-dev \
       libtesseract-dev \
    && rm -rf /var/lib/apt/lists/*

# Environment variable for Tesseract data
ENV TESSDATA_PREFIX=/app

# Symlinks for .NET Tesseract Wrapper
RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
WORKDIR /app/x64
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
RUN ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so
WORKDIR /app

EXPOSE 8082

# Stage 2: Build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the Services project
COPY ["Paperless.Services/Paperless.Services.csproj", "Paperless.Services/"]

# Restore dependencies
RUN dotnet restore "Paperless.Services/Paperless.Services.csproj"

# Copy the source code
COPY Paperless.Services/ Paperless.Services/

WORKDIR "/src/Paperless.Services"
RUN dotnet build "Paperless.Services.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/Paperless.Services"
RUN dotnet publish "Paperless.Services.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app

# Copy published DLLs
COPY --from=publish /app/publish .

# Copy Tesseract DE + EN traineddata directly into the image
RUN mkdir -p /app/tessdata \
    && apt-get update && apt-get install -y curl \
    && curl -L -o /app/tessdata/de.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/de.traineddata \
    && curl -L -o /app/tessdata/en.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/en.traineddata \
    && apt-get remove -y curl \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/*

# Start Service
ENTRYPOINT ["dotnet", "Paperless.Services.dll"]