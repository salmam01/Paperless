# Use the full .NET SDK to both build and run
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS app
WORKDIR /app

# Copy project and restore dependencies
COPY . .
RUN dotnet restore

# Build and publish in one go
RUN dotnet publish -c Release -o out /p:UseAppHost=false

# Install Tesseract and Ghostscript
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    ghostscript \
 && rm -rf /var/lib/apt/lists/*

# Set working directory and entrypoint
WORKDIR /app/out
ENTRYPOINT ["dotnet", "Paperless.Services.dll"]