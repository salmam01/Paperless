# Stage 1
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2
FROM nginx:alpine
# built assets v. build stage kopieren
COPY --from=build /app/dist /usr/share/nginx/html
# nginx konfiguration
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
#  nginx starten
CMD ["nginx", "-g", "daemon off;"]
