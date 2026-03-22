# 🚀 Acquiring System (Payment Gateway Microservices)

[![CI Pipeline](https://github.com/ismailkocak69-ui/AcquiringSystem/actions/workflows/ci-pipeline.yml/badge.svg)](https://github.com/ismailkocak69-ui/AcquiringSystem/actions/workflows/ci-pipeline.yml)

A highly available, fault-tolerant, and scalable Payment Acquiring Gateway built with **.NET 10**. This project demonstrates production-ready **Microservices**, **Event-Driven Architecture (EDA)**, and modern DevOps practices including CI/CD and Container Orchestration.

## 🏗️ Architecture & Key Concepts

- **Microservices Architecture:** Decoupled domains (`Gateway.Api` and `Merchant.Api`) communicating via synchronous (HTTP/Polly) and asynchronous (MassTransit/RabbitMQ) patterns.
- **Event-Driven Architecture (EDA):** Asynchronous background processing for post-authorization events (e.g., Settlement) using **RabbitMQ** (Pub/Sub pattern).
- **Clean Architecture:** The Gateway service is strictly divided into Domain, Application, Infrastructure, and Presentation layers.
- **Resilience & Fault Tolerance:** Advanced network protection using **Polly** (Circuit Breaker, Retry, and Timeout policies) preventing cascading failures during inter-service communication.
- **Idempotency:** Strict duplicate-transaction prevention mechanism ensuring clients are never charged twice for the same network retry.
- **Observability (ELK Stack):** Centralized, structured logging using **Serilog** seamlessly integrated with **Elasticsearch** (Data Streams) and visualized in **Kibana**.
- **Security:** End-to-end endpoint protection using **JWT (JSON Web Token)** authentication.
- **Containerization & Orchestration:** Fully dockerized ecosystem using **Docker Compose** with intelligent `healthcheck` dependencies for seamless, one-click deployments.
- **Test-Driven Development (TDD):** Business logic validated by unit tests using **xUnit, Moq, and FluentAssertions**.
- **CI/CD:** Automated build and test pipelines via **GitHub Actions**.

### ✨ Recent Architectural Enhancements (The "Enterprise" Touch)
- **The Dual-Write Problem Solved (Outbox Pattern):** Implemented the **MassTransit EF Core Outbox Pattern**. This guarantees message delivery to RabbitMQ and ensures strict data consistency across distributed boundaries by combining the database save and event publish into a single atomic transaction.
- **API Hardening (FluentValidation):** Eradicated manual validation logic from controllers. Incoming requests are now strictly validated against business rules using the **FluentValidation** library within the Application layer, enforcing Clean Architecture principles.
- **Database-per-Service (DDD Standard):** Deployed an isolated **PostgreSQL** database (`MerchantDb`) specifically for the Merchant API, ensuring strict domain boundary enforcement with automated EF Core Migrations upon container startup.
- **Performance Optimization (Cache-Aside Pattern):** Integrated an **In-Memory Distributed Cache** within the Merchant API. Validates merchant statuses in milliseconds directly from memory, drastically reducing the load on the PostgreSQL database.

## 💻 Tech Stack

- **Framework:** .NET 10.0 (ASP.NET Core Web API & Minimal APIs)
- **Database:** PostgreSQL (with EF Core Code-First Migrations)
- **Messaging & Consistency:** RabbitMQ, MassTransit, Outbox Pattern
- **Validation:** FluentValidation
- **Caching:** Distributed Memory Cache (Cache-Aside Pattern)
- **Observability:** Elasticsearch, Kibana, Serilog
- **Testing:** xUnit, Moq, FluentAssertions
- **Containerization:** Docker & Docker Compose
- **API UI:** Scalar.AspNetCore (OpenAPI)

## 📂 Project Structure

```text
AcquiringSystem/
│
├── .github/workflows/         # CI/CD Pipeline Definitions
├── docker-compose.yml         # Container Orchestration (API, Postgres, RabbitMQ, ELK)
├── src/
│   ├── Services/
│   │   ├── Gateway/
│   │   │   ├── Gateway.Api/             # API Controllers, JWT, Serilog, Scalar
│   │   │   ├── Gateway.Application/     # Use Cases, Interfaces, DTOs
│   │   │   ├── Gateway.Domain/          # Entities (PaymentTransaction), Events
│   │   │   ├── Gateway.Infrastructure/  # EF Core PostgreSQL DbContext
│   │   │   └── Gateway.UnitTests/       # xUnit TDD Suites
│   │   │
│   │   └── Merchant/
│   │       └── Merchant.Api/            # Minimal API, Merchant Limits Validator
│   │           └── Infrastructure/      # MerchantDb Context & EF Core Migrations
```

## 🚀 Getting Started (One-Click Deployment)

### Prerequisites
Docker Desktop installed and running.

### Spin up the Ecosystem
Navigate to the root directory where `docker-compose.yml` is located and run:

```bash
docker-compose up --build -d
```

> **Note:** The Gateway and Merchant APIs will intelligently wait (via Docker healthchecks) until PostgreSQL and RabbitMQ are fully initialized before starting and applying database migrations.

### Access Points
- **Gateway API (Scalar UI):** http://localhost:5000/scalar
- **RabbitMQ Management:** http://localhost:15672 (guest / guest)
- **Kibana (Logs Dashboard):** http://localhost:5601

## 🔌 API Usage Example

**1. Get JWT Token**
```bash
curl -X GET "http://localhost:5000/api/v1/auth/token"
```
*(Copy the returned token).*

**2. Authorize a Payment**
Use the token in the Authorization header. Sending the exact same payload twice will trigger the Idempotency protection.

```bash
curl -X POST "http://localhost:5000/api/v1/Payments/authorize" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     -d '{
           "idempotencyKey": "A1B2C3D4-E5F6-7890-1234-567890ABCDEF",
           "cardToken": "TOKEN_SUCCESS",
           "amount": 500.00,
           "currency": "TRY",
           "merchantId": "MERCHANT_123"
         }'
```

## 🧪 Running Tests
The project includes a robust unit testing suite to guarantee business rules. To run the tests locally:

```bash
dotnet test src/Services/Gateway/Gateway.UnitTests
```

---
*Architected and built with modern software engineering practices. Ready for production scale.*
