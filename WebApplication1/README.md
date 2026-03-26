# Order Management Service

A robust ASP.NET Core Web API designed for managing the lifecycle of orders. The service follows a layered architecture with a clear separation of concerns, ensuring high performance through caching and reliable state management.

## 🚀 Architecture Overview

The system is built based on **Layered Architecture** principles:
- **API Layer (Controllers):** Handles HTTP requests, JSON parsing, and basic structural validation.
- **Service Layer (Business Logic):** The "brain" of the system. Implements business rules (e.g., state machine transitions) and manages data flow between the database and the cache.
- **Domain Layer:** Contains core entities, enums, and domain-specific exceptions.
- **Data Access Layer (Repository Pattern):** Encapsulates database operations using Entity Framework Core, providing an abstraction over PostgreSQL.

### Key Features:
- **State Machine Logic:** Prevents invalid transitions (e.g., an order cannot be cancelled once it is delivered).
- **Cache-Aside Pattern:** Implemented with **Redis** to provide instant data retrieval for frequent read requests.
- **Optimistic Concurrency:** Uses PostgreSQL `xmin` system column to prevent "lost updates" during parallel requests.
- **Global Error Handling:** Custom Middleware maps domain exceptions to standard HTTP status codes (400, 404, 409).
- **Containerization:** The entire infrastructure (Database & Cache) is orchestrated via Docker Compose.

## 🛠 Tech Stack
- **Framework:** .NET 8 (ASP.NET Core)
- **Database:** PostgreSQL
- **Cache:** Redis
- **ORM:** Entity Framework Core (Npgsql)
- **Infrastructure:** Docker & Docker Compose
- **Documentation:** Swagger / OpenAPI

## 🚦 Getting Started

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- `dotnet-ef` tool (for migrations):
  ```bash
  dotnet tool install --global dotnet-ef

Installation & Setup

    Clone the repository:
    code Bash

    git clone https://github.com/urban-rgb/OrderManagement.git
    cd OrderManagement

    Spin up Infrastructure (PostgreSQL & Redis):
    code Bash

    docker-compose up -d

    Apply Database Migrations:
    code Bash

    dotnet ef database update

    Run the Application:
    code Bash

    dotnet run --project WebApplication1

    The API will be available at https://localhost:7112 (or check your launchSettings.json).

    Explore API Documentation:
    Open https://localhost:7112/swagger in your browser to access the Swagger UI.

📖 API Endpoints Summary

    POST /api/orders - Create a new order.

    GET /api/orders - List orders with pagination support.

    GET /api/orders/{id} - Get detailed order info (cached).

    PATCH /api/orders/{id}/address - Update shipping address (with status validation).

    POST /api/orders/{id}/cancel - Cancel an order.

🛡 Business Rules Implemented

    Orders in InTransit or Delivered status cannot have their shipping address changed.

    Delivered orders cannot be cancelled.

    Optimistic Locking ensures data integrity during concurrent updates.