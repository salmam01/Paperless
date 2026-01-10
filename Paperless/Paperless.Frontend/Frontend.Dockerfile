# Stage 1
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Stage 2
FROM nginx:alpine
# Copy build assets from build stage 
COPY --from=build /app/dist /usr/share/nginx/html
# Configure nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
# Start nginx
CMD ["nginx", "-g", "daemon off;"]
