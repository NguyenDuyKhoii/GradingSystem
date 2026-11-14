<div align="center">

# 🎓 FPTU PE Grading Microservices System

**Automated Practical Exam (PE) Grading, Analytics & Notification Platform for FPT University**  
*Developed for PRN232 – Distributed Applications & Microservices Architecture (.NET 8)*

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker)](https://www.docker.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
[![gRPC](https://img.shields.io/badge/gRPC-High--Performance-244F5D?style=for-the-badge&logo=grpc)](https://grpc.io/)
[![Redis](https://img.shields.io/badge/Redis-Stream%20%26%20PubSub-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Brevo](https://img.shields.io/badge/Brevo-Email%20API-0B996F?style=for-the-badge&logo=maildotru&logoColor=white)](https://www.brevo.com/)

</div>

---

## 📑 Table of Contents
- [1. System Context View](#-1-system-context-view)
- [2. Video Demonstrations](#-2-video-demonstrations)
- [3. Runtime Architecture View](#-3-runtime-architecture-view)
- [4. Communication & Sequence Workflows](#-4-communication--sequence-workflows)
- [5. Deployment View (Docker Architecture)](#-5-deployment-view-docker-architecture)

---

## 🏛️ 1. System Context View

### 1.1 Business Context & Problem Statement
At FPT University, Practical Exams (PE) for essay and project-based subjects (e.g., Project Management, Academic Writing) traditionally rely on tedious manual workflows: downloading `.zip` submissions, toggling across 4 separate windows (Word/PDF $\leftrightarrow$ Rubric $\leftrightarrow$ Excel $\leftrightarrow$ Email), and manually computing weighted scores—leading to calculation errors and operational bottlenecks.

**FPTU PE Grading System** streamlines this workflow through a unified microservices platform:
- **Asynchronous ZIP Ingestion:** Background submission extraction and student metadata parsing powered by **Redis Streams**.
- **Unified Grading Workspace:** Single-screen document preview paired with sub-millisecond weighted score calculation via **gRPC**.
- **Automated Reporting & Delivery:** One-click official university Excel grade sheet export and batch notification via **Brevo Email API**.

### 1.2 System Context Diagram
The diagram below defines the system boundary, participating actors, and external services:

<p align="center">
  <img src="docs/architecture-images/context-diagram.png" alt="System Context Diagram" width="85%" />
</p>

| Category | Entity | Role & Responsibilities |
| :--- | :--- | :--- |
| **Primary Actors** | **Academic Staff** | Manages courses, rubrics, and exam classes; uploads submission ZIPs; exports reports and publishes grades. |
| | **Lecturer** | Evaluates assigned student submissions using interactive rubrics with real-time progress tracking. |
| | **Student** | Receives official grade reports and feedback directly via university email. |
| **External Systems** | **Brevo Email API** | Transactional email gateway for bulk grade notification delivery. |
| | **PostgreSQL 16** | Relational database cluster hosting `FptuAuthDb` and `FptuGradingDb`. |
| | **Redis 7** | Distributed message broker for durable Stream processing and Pub/Sub event channels. |

---

## 📺 2. Video Demonstrations

> *Click each section below to expand and watch the end-to-end walkthrough videos of the platform.*

<details open>
<summary><b>🎥 Demo Video 1: Academic Staff Administration & Asynchronous Processing</b></summary>
<br>

- **Highlights:** Academic Staff login, subject catalog management, multi-criteria rubric configuration, exam class scheduling, and non-blocking ZIP archive extraction powered by **Redis Streams**.

https://github.com/user-attachments/assets/a513bdbd-9348-4565-9e92-7eb159de58b3

</details>

<details>
<summary><b>🎥 Demo Video 2: Lecturer Grading Workspace & Live gRPC Calculation</b></summary>
<br>

- **Highlights:** Lecturer authentication, real-time dynamic progress monitoring ($0/N$), split-screen document preview, interactive criteria evaluation, and sub-millisecond weighted score calculation with automatic letter grade conversion via **gRPC Microservice**.

https://github.com/user-attachments/assets/4625de60-b2f1-4acf-b6ed-af272105376b

</details>

<details>
<summary><b>🎥 Demo Video 3: Grade Analytics, Excel Export & Batch Email Dispatch</b></summary>
<br>

- **Highlights:** Class grade distribution charts (GPA, pass/fail ratios), official FPT University grade sheet export (`EPPlus .xlsx`), and automated batch student notification via **Brevo Transactional Email REST API**.

https://github.com/user-attachments/assets/238e347d-1415-4a04-b8ae-4f8d3b79e79d

</details>

---

## ⚙️ 3. Runtime Architecture View

The system follows a modern **Distributed Microservices Architecture** on the .NET 8 ecosystem, with a centralized API Gateway serving as the single entry point for all client requests.

### 3.1 Service Catalog

| Service Name | Technology Stack | Host Port | Internal Port | Primary Responsibilities |
| :--- | :--- | :---: | :---: | :--- |
| **Gateway Service** | .NET 8 / YARP Reverse Proxy | `8000` | `8080` | Single entry point, routing, reverse proxying, CORS management, and WebSocket proxying. |
| **Auth Service** | .NET 8 / EF Core | `5002`, `5003` | `8080`, `8081` | Identity Provider: User management, authentication, role-based authorization (`AcademicStaff`, `Lecturer`), JWT token issuance, and gRPC user synchronization. |
| **Grading Service** | .NET 8 / MediatR / CQRS | `5000` | `8080` | Core domain: Exam classes, student submissions, rubric criteria, Excel grade report generation (EPPlus), and grade publication. |
| **Score Calculator Service** | .NET 8 / gRPC (HTTP/2) | `5001` | `8080` | High-performance calculation engine: Reading submission files, computing weighted scores, evaluating Pass/Fail, and letter grade conversion. |
| **Worker Service** | .NET 8 / BackgroundService | — | — | Background task processing: Decompressing ZIP archives via Redis Streams, generating periodic 5-minute analytics reports, and sending emails via Brevo API. |
| **PostgreSQL Database** | PostgreSQL 16 Alpine | `5432` | `5432` | Relational storage for application data across `FptuAuthDb` and `FptuGradingDb`. |
| **Redis Broker & Cache** | Redis 7 Alpine | `6379` | `6379` | In-memory message broker managing durable Streams (FIFO) and Pub/Sub event channels. |
| **Frontend Application** | React 19 / Vite | `5173` | `5173` | Responsive Single Page Application (100% English, real-time dynamic progress tracking, glassmorphic UI). |

---

## 🔄 4. Communication & Sequence Workflows

### 4.1 Inter-Service Communication Matrix

| Source Service | Target Service | Protocol / Transport | Purpose |
| :--- | :--- | :--- | :--- |
| **Frontend Web** | **YARP Gateway** | HTTP/1.1 REST (`Bearer JWT`) | All client interactions and state mutations. |
| **YARP Gateway** | **Auth / Grading API** | HTTP/1.1 Reverse Proxy | Route matching and request forwarding. |
| **GradingService** | **ScoreCalculator** | **gRPC (HTTP/2 + Protobuf)** | Sub-millisecond arithmetic score calculation and letter grade mapping. |
| **GradingService** | **AuthService** | **gRPC (HTTP/2)** | Verify and synchronize lecturer profiles during class scheduling. |
| **GradingService** | **Redis (Stream)** | RESP3 (`XADD`) | Enqueue compute-heavy ZIP extraction jobs (`zip-processing-stream`). |
| **Worker Service** | **Redis (Stream)** | RESP3 (`XREADGROUP` / `XACK`) | Durable consumer group processing with guaranteed at-least-once delivery. |
| **GradingService** | **Redis (Pub/Sub)** | RESP3 (`PUBLISH`) | Broadcast class grade publication events (`grade-published-channel`). |
| **Worker Service** | **Brevo REST API** | HTTPS REST (`api-key`) | Deliver batch transactional emails to students. |

### 4.2 Core Sequence Workflows

#### Workflow A: End-to-End Submission Evaluation & Grade Publication
Illustrates the complete operational lifecycle: from ZIP archive upload and background Redis Stream decompression to live gRPC score evaluation, final grade publication, and Brevo email notification:

<p align="center">
  <img src="docs/architecture-images/sequence-core-workflow.jpg" alt="End-to-End Workflow Diagram" width="85%" />
</p>

#### Workflow B: Interactive Rubric Evaluation with gRPC Calculator
Demonstrates the real-time interaction between the frontend, Grading Service, and isolated gRPC Score Calculator:

<p align="center">
  <img src="docs/architecture-images/sequence-grading-grpc.png" alt="gRPC Grading Sequence Diagram" width="85%" />
</p>

#### Workflow C: Batch Grade Publication & Email Dispatch
Illustrates how publishing an exam class triggers asynchronous event broadcasting via Redis Pub/Sub to dispatch official grade notifications:

<p align="center">
  <img src="docs/architecture-images/sequence-publish-email.png" alt="Publish and Email Sequence Diagram" width="85%" />
</p>

---

## 🐳 5. Deployment View (Docker Architecture)

The entire backend infrastructure is containerized and orchestrated via **Docker Compose**, running within an isolated bridge network (`grading-network`):

<p align="center">
  <img src="docs/architecture-images/deployment-diagram.png" alt="Deployment Architecture Diagram" width="85%" />
</p>

### 5.1 Container Topology & Port Mapping

| Container Name | Service Image | Host Port | Container Port | Purpose / Protocol |
| :--- | :--- | :---: | :---: | :--- |
| `grading-gateway` | `be-gateway` | `8000` | `8080` | Single entry point for frontend HTTP & WebSocket traffic. |
| `grading-auth-api` | `be-auth-api` | `5002`, `5003` | `8080`, `8081` | REST Auth API (`5002`) & gRPC User Sync Service (`5003`). |
| `grading-api` | `be-api` | `5000` | `8080` | Core RESTful Grading API & SignalR hub. |
| `grading-score-calculator-service` | `be-score-calculator` | `5001` | `8080` | gRPC Score Calculation Engine (HTTP/2). |
| `grading-worker` | `be-worker` | — | — | Background task processor (Redis Streams, Pub/Sub, Brevo API). |
| `grading-postgres` | `postgres:16-alpine` | `5432` | `5432` | Relational database (`FptuGradingDb` & `FptuAuthDb`). |
| `grading-redis` | `redis:7-alpine` | `6379` | `6379` | Message broker & distributed cache. |

### 5.2 Shared Storage Volumes
- **`postgres-data`** (`/var/lib/postgresql/data`): Persistent storage volume for PostgreSQL databases.
- **`uploads-data`** (`/app/uploads`): Shared named volume mounted across `grading-api`, `grading-worker`, and `grading-score-calculator-service` for synchronized ZIP archives and extracted student files.
