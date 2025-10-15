# Paperless - Instructions
# From the project root, move one folder down
- cd Paperless

# Make sure to start Docker Desktop, then build the containers
- docker compose build

# Start the containers
- docker compose up -d

# Instead (Optional): One-liner
cd Paperless
docker compose build
docker compose up -d

# Now the application should be accessible via localhost
- Frontend: http://localhost:80
- Backend: http://localhost:8080
- RabbitMQ: http://localhost:15672