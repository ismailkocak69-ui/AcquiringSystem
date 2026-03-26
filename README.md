# 🚀 Acquiring System - Enterprise FinTech Architecture

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-000000?style=for-the-badge&logo=opentelemetry&logoColor=white)

This repository contains a **production-ready, enterprise-grade microservices ecosystem** designed for financial payment processing (Acquiring). Built with modern .NET architecture, it demonstrates advanced distributed system patterns, high-performance security measures, and end-to-end observability.

## 🌟 Key Architectural Highlights

* **API Gateway (YARP):** All internal microservices are isolated from the public internet. YARP acts as the single entry point (BFF) handling routing and load balancing.
* **Rate Limiting & Defense:** IP-based rate limiting is enforced at the Gateway layer to protect the system against DDoS and brute-force attacks.
* **Ultra-Fast Idempotency:** Duplicate payment requests are intercepted at the In-Memory/Distributed Cache layer, saving expensive database read operations and reducing latency to milliseconds.
* **Clean Architecture & CQRS:** Business logic is decoupled using **MediatR**, ensuring strict adherence to SOLID and Separation of Concerns (SoC) principles.
* **Distributed Data Consistency:** * **Transactional Outbox Pattern** ensures messages are never lost between PostgreSQL and RabbitMQ.
  * **MassTransit Saga State Machine** orchestrates long-running distributed transactions, providing "Compensating Transactions" to maintain Eventual Consistency if downstream services fail.
* **End-to-End Observability:** **OpenTelemetry** and **Jaeger** are integrated natively (via Npgsql and MassTransit hooks) to trace requests, database queries, and message broker latencies across the entire ecosystem.
* **Automated CI/CD:** A robust GitHub Actions pipeline runs unit tests, builds the solution, and pushes Docker images to the GitHub Container Registry (GHCR) on every commit.

---

## 📂 Project Structure

The solution follows a strict Clean Architecture pattern, divided into microservices and an API Gateway.

```text
AcquiringSystem/
├── src/
│   ├── ApiGateways/
│   │   └── YarpApiGateway/         # The central entry point, routing, and Rate Limiting
│   ├── Services/
│   │   ├── Gateway/                # Core Payment Processing API
│   │   │   ├── Gateway.Api         # Presentation Layer (Controllers, Middlewares)
│   │   │   ├── Gateway.Application # Application Layer (CQRS Handlers, Saga State Machine)
│   │   │   ├── Gateway.Domain      # Domain Layer (Entities, Events, Exceptions)
│   │   │   └── Gateway.Infrastructure # Data Access Layer (PostgreSQL, Outbox, Repositories)
│   │   ├── Merchant/               # Merchant Management & Validation API
│   │   │   ├── Merchant.Api
│   │   │   └── ... (Clean Arch layers)
├── tests/
│   └── Gateway.UnitTests/          # xUnit & Moq based isolated tests
├── .github/workflows/              # CI/CD Pipeline configurations
├── docker-compose.yml              # Infrastructure orchestration
└── README.md
```

# 🛠️ Tech Stack

* Core: .NET 10.0, C#
* Architecture: Microservices, Clean Architecture, CQRS, Saga Pattern, Outbox Pattern
* Libraries: MediatR, MassTransit, FluentValidation, YARP
* Databases & Caching: PostgreSQL, Redis / In-Memory Cache
* Message Broker: RabbitMQ
* Observability: OpenTelemetry, Jaeger
* DevOps: Docker, Docker Compose, GitHub Actions, GHCR

## 🚀 Getting Started (Local Development)

### Prerequisites
* Docker Desktop
* .NET 10 SDK

### Installation & Run
1. Clone the repository:
```bash
git clone [https://github.com/ismailkocak69-ui/AcquiringSystem.git](https://github.com/ismailkocak69-ui/AcquiringSystem.git)
cd AcquiringSystem
```
2. Spin up the infrastructure and microservices:
```bash
docker-compose up -d --build
```
This command will start PostgreSQL, RabbitMQ, Jaeger, YARP, Gateway API, and Merchant API.

3. Verify the services:
     * YARP API Gateway: http://localhost:8000
     * Jaeger UI: http://localhost:16686
     * RabbitMQ Management: http://localhost:15672 (guest / guest)
  
## 📡 API Guide & Testing
All external traffic must pass through the YARP Gateway (localhost:8000). Direct access to internal microservices is blocked by design.

1. Authorize a Payment
```HTTP
POST http://localhost:8000/api/v1/payments/authorize
Content-Type: application/json
{
  "merchantId": "12345",
  "cardNumber": "4545-4545-4545-4545",
  "amount": 150.00,
  "idempotencyKey": "unique-guid-here"
}
```
2. Test Rate Limiting
Spam the endpoint above more than 5 times within 10 seconds. You will receive a 429 Too Many Requests response, proving the YARP defense shield is active.

3. Test Idempotency
Send the exact same request with the same idempotencyKey multiple times. The system will return the cached success response instantly without hitting the database, logging an Idempotency Cache HIT!.

4. Test Saga & Compensating Transactions
(Simulating a downstream Settlement failure)
```HTTP
POST http://localhost:8000/api/v1/payments/test-settlement-fail/{transactionId}
```
Watch the logs or database as the Saga State Machine intercepts the failure, automatically triggers a CancelPaymentMessage, and perfectly restores data consistency by marking the transaction as Refunded.

## 🔬 Observability with Jaeger
Stop guessing where the bottleneck is. Open your browser and navigate to:
#### 👉 http://localhost:16686
1. Select Yarp.Gateway or Gateway.Api from the Service dropdown.
2. Click Find Traces.
3. Inspect the Gantt chart to see the exact millisecond latency of YARP routing, PostgreSQL executions, and RabbitMQ message publishing.
---
*Architected and developed with adherence to modern software engineering standards.*

Developed by: İsmail Koçak
