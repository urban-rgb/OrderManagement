# OrderManagement

A full-stack order management system built with ASP.NET Core and Angular.

## Tech Stack

**Backend:** ASP.NET Core 10, Entity Framework Core, PostgreSQL, Redis, Mapster  
**Frontend:** Angular 21, TypeScript  
**Infrastructure:** Docker, Docker Compose, Nginx

## Getting Started

### Prerequisites

- Docker Desktop

### Run

Clone the repository, then start the backend:

```bash
cd WebApplication1
docker compose up -d --build
```

Start the frontend:

```bash
cd frontend
docker compose up -d --build
```

- Frontend: http://localhost:4200
- Swagger UI: http://localhost:8080/swagger

## Features

- Create and view orders
- Filter and sort order list
- Update shipping address
- Cancel orders
- Redis caching
- Automatic database migrations on startup

## Project Structure

```
OrderManagement/
├── WebApplication1/     # ASP.NET Core backend
│   ├── Controllers/
│   ├── Domain/
│   ├── Services/
│   ├── Data/
│   ├── Migrations/
│   └── Middleware/
└── frontend/            # Angular frontend
    └── src/
        └── app/
            ├── core/
            ├── features/
            └── shared/
```