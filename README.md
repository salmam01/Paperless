# Paperless

A document management system for uploading, processing, and organizing documents with automatic text recognition (OCR) and AI-based summarization.

## Overview

Paperless allows users to upload PDF, DOCX, and TXT documents. The system automatically extracts text using OCR, generates summaries using AI, assigns categories, and provides full-text search capabilities.

## Architecture

The application consists of five main components:

- **Frontend**: React with TypeScript, served via nginx
- **REST API**: ASP.NET Core backend with PostgreSQL database
- **Services**: Background workers for OCR, AI processing, and ElasticSearch indexing
- **Batch**: Scheduled processor for access data logs
- **Infrastructure**: Docker containers for PostgreSQL, RabbitMQ, MinIO, and ElasticSearch

## Prerequisites

To be able to run this application, you need:

- Docker Desktop installed and running
- At least 8GB of available RAM
- Ports 80, 8080, 5432, 5672, 9000, 9090, 9200, 5601, 15672 available

## Build Instructions

From the project root, move one folder down:

```bash
cd Paperless
```

Make sure to start Docker Desktop, then build the containers:

```bash
docker compose build
```

Start the containers:

```bash
docker compose up -d
```

Alternatively, use a one-liner:

```bash
cd Paperless && docker compose build && docker compose up -d
```

## Accessing the Application

The application should be accessible via localhost:

- Frontend: http://localhost:80
- Backend: http://localhost:8080
- RabbitMQ Management: http://localhost:15672
- MinIO Console: http://localhost:9090
- Kibana: http://localhost:5601

## Stop Services

To stop all containers:

```bash
docker compose down
```

To stop and remove volumes:

```bash
docker compose down -v
```

## Features

- Upload documents (PDF, DOCX, TXT) 
- Automatic OCR text extraction from PDFs
- AI-powered document summarization
- Automatic category assignment
- Full-text search with fuzzy matching
- Category management
- Document metadata viewing

## Technology Stack

- Frontend: React, TypeScript, Vite, nginx
- Backend: C# .NET 8.0, ASP.NET Core, Entity Framework Core
- Database: PostgreSQL 16
- Storage: MinIO (S3-compatible)
- Search: ElasticSearch 8.9.0
- Message Queue: RabbitMQ 3
- OCR: Tesseract 5.2.0
- AI: Generative AI service for summarization

## Development

### Running Tests

Unit tests:

```bash
dotnet test Tests/UnitTests/
```

Integration tests:

```bash
dotnet test Tests/IntegrationTests/
```

### Project Structure

```
Paperless/
├── Paperless.Frontend/     # React frontend application
├── Paperless.REST/         # REST API (API, BL, DAL layers)
├── Paperless.Services/     # Background workers
├── Paperless.Batch/        # Batch processing
└── Tests/                  # Unit and integration tests
```


## Troubleshooting

If containers fail to start:

1. Check Docker Desktop is running
2. Verify ports are not in use
3. Check container logs: `docker compose logs [service-name]`
4. Ensure sufficient disk space and memory

If documents are not processing:

1. Check RabbitMQ Management UI for message queues
2. Verify workers are running: `docker compose ps`
3. Check service logs: `docker compose logs paperless-services`

## License

This project was developed as part of Software Engineering 3 course at University of Applied Sciences Technikum Wien.
