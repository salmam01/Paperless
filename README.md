# Paperless

# From the project root, go into the frontend folder and install node_modules
- cd Paperless/Paperless.Client/paperless-client
- npm install

# Move back to the root folder and build docker containers
- cd ../../..
- docker compose build
- docker compose up -d

# One-liner (copy & paste)
cd Paperless/Paperless.Client/paperless-client
npm install
cd ../../..
docker compose build
docker compose up -d

# Now the application should be accessible via localhost
- frontend: http://localhost:80
- backend: http://localhost:8080