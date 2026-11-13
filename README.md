# 🎓 FPTU PE Grading Microservices System
> **Automated Practical Exam (PE) Grading, Analytics & Notification Platform for FPT University**  
> *Developed for PRN232 – Distributed Applications & Microservices Architecture (.NET 8)*

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![gRPC](https://img.shields.io/badge/gRPC-High--Performance-244F5D?logo=grpc)](https://grpc.io/)
[![Redis](https://img.shields.io/badge/Redis-Stream%20%26%20PubSub-DC382D?logo=redis)](https://redis.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Brevo](https://img.shields.io/badge/Brevo-Email%20API-0B996F?logo=maildotru)](https://www.brevo.com/)

---

## 📺 Video Demonstrations

> *This section contains 3 detailed demonstration videos covering each operational phase and business scenario of the system:*

### 🎥 Demo Video 1: Academic Staff Exam Administration & Asynchronous Processing
* **Highlights:** Academic Staff authentication, subject management (`Subject`), multi-criteria grading rubric configuration (`Rubric Criteria`), exam class scheduling, and batch submission ZIP file upload with non-blocking extraction powered by **Redis Streams**.
* **Video URL:** *[👉 PASTE DEMO VIDEO 1 LINK HERE (YouTube / Google Drive)]()*
<!-- Video 1 Placeholder -->
```
[VIDEO 1 PLACEHOLDER: Academic Staff Setup, Rubric Management & Redis Streams Extraction]
```

---

### 🎥 Demo Video 2: Lecturer Grading & High-Performance gRPC Score Calculation
* **Highlights:** Lecturer login, real-time dynamic progress monitoring ($0/N$), split-screen submission inspection, interactive rubric grading, and sub-millisecond weighted score calculation with automatic letter grade conversion via **gRPC Microservice**.
* **Video URL:** *[👉 PASTE DEMO VIDEO 2 LINK HERE (YouTube / Google Drive)]()*
<!-- Video 2 Placeholder -->
```
[VIDEO 2 PLACEHOLDER: Lecturer Grading & gRPC Score Calculation Engine]
```

---

### 🎥 Demo Video 3: Grade Analytics, Excel Export & Batch Transactional Email Dispatch
* **Highlights:** Class grade distribution analytics (GPA, Pass/Fail ratios, grade charts), standardized FPT University grade sheet export (`EPPlus .xlsx`), and automated batch student notification via **Brevo Transactional Email REST API**.
* **Video URL:** *[👉 PASTE DEMO VIDEO 3 LINK HERE (YouTube / Google Drive)]()*
<!-- Video 3 Placeholder -->
```
[VIDEO 3 PLACEHOLDER: Analytics, Excel Report Generation & Brevo Email Dispatch]
```

---

## 🏛️ 1. System Context View

### 1.1 Business Context & Problem Statement
At FPT University, Practical Exams (PE) for non-programming or text-heavy subjects (such as Academic Writing, Business Communication, Project Management, and Entrepreneurship) require students to submit written documents packaged in compressed `.zip` archives. 

Under the traditional grading workflow, lecturers endure a tedious and error-prone process:
* **The "Alt-Tab" Bottleneck:** Lecturers must constantly toggle between 4 separate application windows (Submission Document $\leftrightarrow$ PDF Rubric Guide $\leftrightarrow$ Master Excel Grade Template $\leftrightarrow$ Email Client).
* **Manual Arithmetic Errors:** Manually summing weighted criteria scores often leads to calculation discrepancies, especially under tight grading deadlines.
* **Student Identity Confusion:** Managing hundreds of local files increases the risk of recording a score against the wrong student ID.

**FPTU PE Grading System** addresses these pain points through a unified, cloud-native distributed architecture:
1. **Asynchronous Archive Extraction:** Uploaded `.zip` archives are unpacked in the background using **Redis Streams**, extracting student metadata and generating official FPT University email addresses.
2. **Single-Screen Grading Workspace:** A unified interface featuring document preview alongside interactive rubric sliders.
3. **Automated Arithmetic via gRPC:** Total scores, letter grades, and pass/fail statuses are computed instantly by an isolated calculation microservice.
4. **Standardized Reporting & Automated Outreach:** One-click Excel grade report generation matching official university formats, accompanied by bulk email delivery via Brevo API upon grade publication.

### 1.2 System Context Diagram

```mermaid
graph TD
    subgraph Users ["System Actors"]
        A["Academic Staff<br/>(Examinations / Administration)"]
        L["Lecturer<br/>(Exam Evaluator)"]
        S["Student<br/>(Examinee)"]
    end

    subgraph System ["FPTU PE Grading Microservices System"]
        GW["YARP API Gateway<br/>(Port 8000)"]
    end

    subgraph External ["External Infrastructure & Cloud Providers"]
        BREVO["Brevo Email Service<br/>(Transactional REST API)"]
        POSTGRES[("PostgreSQL 16<br/>Relational Database")]
        REDIS[("Redis 7<br/>Streams & Pub/Sub Broker")]
    end

    A -->|1. Setup Subjects, Rubrics, Classes & Upload Submissions ZIP| GW
    L -->|2. Inspect Submissions, Evaluate via Rubric & Submit Grades| GW
    GW --> POSTGRES
    GW --> REDIS
    REDIS -->|Worker dispatches bulk grade notification emails| BREVO
    BREVO -->|3. Receive formal grade report & lecturer feedback| S
```

---

## ⚙️ 2. Runtime Architecture & Microservices

The application is engineered on the **.NET 8** ecosystem following modern distributed systems best practices:

```mermaid
graph TB
    subgraph Client ["Client Presentation Layer"]
        FE["React 19 + Vite SPA<br/>(Port 5173)"]
    end

    subgraph Gateway ["Reverse Proxy & Edge Gateway"]
        YARP["YARP API Gateway<br/>(Port 8000)"]
    end

    subgraph Microservices ["Microservices Core"]
        AUTH["AuthService (REST + gRPC)<br/>Port 5002 (HTTP) / 5003 (gRPC)"]
        GRADING["GradingService (REST API)<br/>Port 5000 (HTTP)"]
        CALC["ScoreCalculatorService (gRPC)<br/>Port 5001 (gRPC / HTTP)"]
        WORKER["Worker Service (Background)<br/>(Redis Streams & Pub/Sub Consumer)"]
    end

    subgraph Data ["Data & Messaging Layer"]
        DB[("PostgreSQL 16<br/>Port 5432")]
        CACHE[("Redis 7<br/>Port 6379")]
    end

    subgraph ThirdParty ["External Cloud Provider"]
        EMAIL["Brevo REST API<br/>(api.brevo.com/v3/smtp/email)"]
    end

    FE -->|HTTP / RESTful Requests| YARP
    YARP -->|Proxy /api/auth| AUTH
    YARP -->|Proxy /api/exam-classes, /api/submissions...| GRADING

    GRADING -->|gRPC High-Performance RPC| CALC
    GRADING -->|Sync User gRPC| AUTH
    GRADING -->|Entity Framework Core| DB
    AUTH -->|Entity Framework Core| DB

    GRADING -->|Enqueue Task 'zip-processing-stream'| CACHE
    GRADING -->|Publish Event 'grade-published-channel'| CACHE

    CACHE -->|Stream Consumer XREADGROUP| WORKER
    CACHE -->|Pub/Sub Subscriber| WORKER
    WORKER -->|Dispatch Batch Emails| EMAIL
    WORKER -->|Persist Submissions & Status Updates| DB
    WORKER -->|5-Min Periodic Analytics Push| CACHE
```

### 2.1 Service Catalog

| Service Name | Technology Stack | Internal Port | Host Port | Primary Responsibilities |
| :--- | :--- | :---: | :---: | :--- |
| **YARP Gateway** | .NET 8 / YARP | 8080 | **8000** | Centralized API entry point, route dispatching, CORS handling, rate limiting. |
| **AuthService** | .NET 8 / EF Core | 8080, 8081 | **5002, 5003** | User authentication, JWT issuance, role management (`AcademicStaff` / `Lecturer`), gRPC identity synchronization. |
| **GradingService** | .NET 8 / MediatR / CQRS | 8080 | **5000** | Core business domain: Course catalog, exam scheduling, rubric criteria, Excel grade sheet export via EPPlus. |
| **ScoreCalculator** | .NET 8 / gRPC | 8080 | **5001** | High-speed arithmetic engine: Weighted criteria score computation, letter grade mapping (A+/A/B/C/D/F), pass/fail evaluation. |
| **Worker Service** | .NET 8 / BackgroundService | - | - | Asynchronous consumer: Background ZIP extraction via Redis Streams, periodic 5-minute analytics generation, transactional email dispatch via Brevo. |
| **PostgreSQL 16** | Postgres Alpine | 5432 | **5432** | Primary ACID-compliant relational data store. |
| **Redis 7** | Redis Alpine | 6379 | **6379** | High-performance message broker supporting durable FIFO Streams and low-latency Pub/Sub channels. |
| **Frontend Web** | React 19 / Vite | 5173 | **5173** | Modern Single Page Application (100% English, responsive layout, real-time dynamic progress tracking). |

---

## 🔄 3. Communication Matrix & Technical Workflows

### 3.1 Inter-Service Communication Matrix

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

---

### 3.2 Workflow 1: Asynchronous ZIP Upload & Background Extraction (Redis Stream)

```mermaid
sequenceDiagram
    autonumber
    actor A as Academic Staff
    participant GW as YARP Gateway
    participant API as GradingService.API
    participant R as Redis Stream
    participant W as Worker Service
    participant DB as PostgreSQL

    A->>GW: POST /api/exam-classes/{id}/upload-zip (Multipart .zip file)
    GW->>API: Route to UploadExamClassZipCommand
    API->>API: Save archive to shared storage volume (/app/uploads)
    API->>R: XADD zip-processing-stream (ExamClassId, ZipFilePath, ExtractPath)
    API->>DB: Set ExamClass.Status = 'Processing'
    API-->>A: 200 OK ("File uploaded, extracting in background...")

    Note over R,W: Asynchronous Decoupled Processing
    W->>R: XREADGROUP poll for new work
    W->>W: Extract ZIP archive to /app/uploads/exam-classes/{id}/
    W->>W: Parse StudentId and StudentName from directory/file naming conventions
    W->>W: Generate canonical FPT student email format
    W->>DB: Bulk INSERT Submissions & Set ExamClass.Status = 'Grading'
    W->>R: XACK & XDEL to confirm successful job completion
```

---

### 3.3 Workflow 2: Interactive Rubric Evaluation with gRPC Calculator

```mermaid
sequenceDiagram
    autonumber
    actor L as Lecturer
    participant FE as React Frontend
    participant GW as YARP Gateway
    participant API as GradingService.API
    participant CALC as ScoreCalculator (gRPC)
    participant DB as PostgreSQL

    L->>FE: Adjust criteria slider or input point value
    FE->>GW: POST /api/grades/calculate (CriterionScores & Weights)
    GW->>API: Route to CalculateScoreEndpoint
    API->>CALC: gRPC CalculateGrade(ScoreRequest)
    Note over CALC: Calculate: TotalScore = Σ(Score * Weight / MaxPoints)<br/>Determine Letter Grade (A+/A/B/C/D/F) & isPassed
    CALC-->>API: gRPC ScoreResponse (TotalScore, LetterGrade, isPassed)
    API-->>FE: Instantaneous JSON response updating calculated totals on UI

    L->>FE: Click "Submit & Publish"
    FE->>GW: POST /api/grades (Persist grade & feedback)
    GW->>API: SaveGradeCommand
    API->>DB: Save Grade, GradeDetails & Set Submission.Status = 'Graded'
    API-->>FE: Update real-time class progress bar (GradedCount/TotalSubmissions)
```

---

### 3.4 Workflow 3: Bulk Grade Publication & Transactional Email Dispatch

```mermaid
sequenceDiagram
    autonumber
    actor A as Academic Staff
    participant FE as React Frontend
    participant API as GradingService.API
    participant R as Redis Pub/Sub
    participant W as Worker Service
    participant BREVO as Brevo Email API
    actor S as Student

    A->>FE: Click "Send Mail" (Publish Grades)
    FE->>API: POST /api/exam-classes/{id}/publish-grades
    Note over API: Validation: Require all submissions to be 100% graded
    API->>API: Set ExamClass.Status = 'Completed'
    API->>R: PUBLISH 'grade-published-channel' (Class metadata & student grades)
    API-->>FE: 200 OK ("Grades published, batch emails are being sent")

    R->>W: Trigger OnMessageReceived callback
    loop Each student in exam class
        W->>W: Render responsive HTML email (Scores, Letter Grade, Evaluator Comments)
        W->>BREVO: POST https://api.brevo.com/v3/smtp/email (Payload JSON)
        BREVO-->>S: Direct inbox delivery to student email address
    end
```

---

## 🐳 4. Deployment View (Docker Containerization)

The entire microservices topology is orchestrated through **Docker Compose** on an isolated bridge network:

```
+-----------------------------------------------------------------------------------+
|                            Docker Host (grading-network)                          |
|                                                                                   |
|  [Port 8000] ------> grading-gateway (YARP Reverse Proxy)                         |
|                           |                                                       |
|           +---------------+---------------+                                       |
|           |                               |                                       |
|  [Port 5002/5003]                 [Port 5000]                                     |
|  grading-auth-api (AuthService)   grading-api (GradingService)                    |
|           |                               |                                       |
|           |                               +----------> grading-score-calc [5001]  |
|           |                               |            (gRPC Calculator)          |
|           |                               |                                       |
|           +-----------------------+-------+                                       |
|                                   |                                               |
|                    +--------------+--------------+                                |
|                    |                             |                                |
|           [Port 5432]                       [Port 6379]                           |
|           grading-postgres                  grading-redis                         |
|           (PostgreSQL 16)                   (Redis 7 Stream/PubSub)               |
|                    |                             |                                |
|                    |                             v                                |
|                    +-------------------> grading-worker                           |
|                                          (Background Queue & Brevo Consumer)      |
|                                                  |                                |
|                                                  v                                |
|                                            [Internet] ---> Brevo Email API        |
+-----------------------------------------------------------------------------------+
|  Volumes:                                                                         |
|    - postgres-data: /var/lib/postgresql/data (Persistent database storage)        |
|    - uploads-data:  /app/uploads (Shared submission archives & extracted files)   |
+-----------------------------------------------------------------------------------+
```

### 4.1 Port Mapping & Service Dependencies

| Container Name | Service Image | Host Port | Container Port | Dependencies |
| :--- | :--- | :---: | :---: | :--- |
| `grading-gateway` | `be-gateway` | **8000** | 8080 | `auth-api`, `api` |
| `grading-auth-api` | `be-auth-api` | **5002, 5003** | 8080, 8081 | `postgres` |
| `grading-api` | `be-api` | **5000** | 8080 | `postgres`, `redis`, `score-calculator-service` |
| `grading-score-calculator-service` | `be-score-calculator` | **5001** | 8080 | None |
| `grading-worker` | `be-worker` | - | - | `postgres`, `redis` |
| `grading-postgres` | `postgres:16-alpine` | **5432** | 5432 | None |
| `grading-redis` | `redis:7-alpine` | **6379** | 6379 | None |

---

## 🚀 5. Quick Start Guide

### 📋 Prerequisites
1. **Docker Desktop** installed and running.
2. **Node.js** (v18.x or v20.x LTS).
3. **Git**.

---

### 🔹 Step 1: Clone the Repository
```bash
git clone https://github.com/NguyenDuyKhoii/GradingSystem.git
cd GradingSystem
```

---

### 🔹 Step 2: Configure Environment Variables (`be/.env`)
Create a `.env` file inside the `be` directory (or duplicate from `.env.example`):
```env
# Brevo Email Configuration
BREVO_API_KEY=your_brevo_api_key_here
BREVO_SENDER_EMAIL=khoind1235@gmail.com
```

---

### 🔹 Step 3: Launch Microservices via Docker Compose
Open a terminal in the `be` folder:
```bash
cd be
docker compose up -d --build
```
> ⏱️ *The initial build takes approximately 1-2 minutes. Verify that all 7 containers are active with `docker ps`.*

---

### 🔹 Step 4: Launch the Frontend Web Application
Open a new terminal window in the `fe` directory:
```bash
cd fe
npm install
npm run dev
```
Navigate to **`http://localhost:5173`** in your browser.

---

### 🔑 Step 5: Default Seed Accounts

| Role | Username | Password | Full Name | Demo Purpose |
| :--- | :--- | :---: | :--- | :--- |
| **Academic Staff** | `academic` | `12345` | **Academic Staff** | Manage subjects, configure rubrics, schedule exam classes, upload ZIP submissions, export Excel reports, dispatch bulk emails. |
| **Lecturer** | `lecturer1` | `12345` | **ThS. Nguyễn Duy Khôi** | View assigned exam classes, inspect student submissions, grade via interactive rubrics with live gRPC score calculation. |
| **Lecturer** | `lecturer2` | `12345` | **TS. Lê Hoàng Nam** | Evaluator account for concurrent grading scenarios. |
| **Lecturer** | `lecturer3` | `12345` | **TS. Trần Trọng Bình** | Evaluator account. |
| **Lecturer** | `lecturer4` | `12345` | **ThS. Huỳnh Tuấn Anh** | Evaluator account. |
| **Lecturer** | `lecturer5` | `12345` | **TS. Kiều Trọng Khánh** | Evaluator account. |

---

## 📂 6. Project Directory Structure

```
GradingSystem2/
├── be/                                      # Microservices Backend (.NET 8)
│   ├── docker-compose.yml                   # Docker Compose orchestrating 7 containers
│   ├── .env.example                         # Environment template for Brevo Email configuration
│   ├── FptuGradingSystem.Gateway/           # YARP Reverse Proxy & Edge Gateway
│   ├── FptuGradingSystem.AuthService/       # Authentication API (JWT + gRPC Identity Sync)
│   ├── FptuGradingSystem.GradingService/    # Core Grading Service (Clean Architecture / CQRS)
│   │   ├── FptuGradingSystem.API/           # RESTful Controllers (Kebab-case routing)
│   │   ├── FptuGradingSystem.Application/   # MediatR Commands, Queries, Validators, DTOs
│   │   ├── FptuGradingSystem.Domain/        # Core Business Entities (ExamClass, Submission, Grade...)
│   │   └── FptuGradingSystem.Infrastructure/# EF Core DbContext, PostgreSQL Migrations
│   ├── FptuGradingSystem.ScoreCalculatorService/ # gRPC Service Engine (.proto contract)
│   └── FptuGradingSystem.Worker/            # Background Worker (Redis Streams, Pub/Sub, Brevo API)
├── fe/                                      # Frontend Client (React 19 + Vite)
│   ├── src/
│   │   ├── components/
│   │   │   ├── AcademicDashboard.jsx        # Academic Staff Portal (Subjects, Rubrics, Classes, Analytics)
│   │   │   ├── LecturerDashboard.jsx        # Lecturer Portal (Real-time Progress 0/N, Submissions List)
│   │   │   ├── GradingView.jsx              # Split-screen Grading Workspace & gRPC Live Calculation
│   │   │   └── Login.jsx                    # JWT Authentication Component
│   │   ├── services/api.js                  # Axios Interceptor with Bearer Token Injection
│   │   └── index.css                        # Modern Glassmorphic UI Stylesheet
├── test-data/                               # Seed Submissions Archive for Demonstration
│   └── SE1801_Submissions.zip               # Sample ZIP containing 5 student submissions
└── README.md                                # System Architecture & Deployment Documentation
```

---

## 📋 7. PRN232 Course Assessment Requirement Mapping

| Mandatory Requirement | Architectural Implementation | Source Code Reference |
| :--- | :--- | :--- |
| **1. REST API Service** | ASP.NET Core 8 Web API with standardized kebab-case endpoints, dynamic pagination, multi-field sorting, filtering, and JWT authentication. | `GradingService.API/Controllers/`, `AuthService.API/` |
| **2. Background Job** | Dedicated Worker Service running periodic 5-minute analytics generation and asynchronous batch email processing. | `Worker/GradeReportWorker.cs`, `Worker/Services/BrevoEmailService.cs` |
| **3. Message Broker** | Redis Streams (durable FIFO queue for ZIP archive processing) & Redis Pub/Sub (real-time publication broadcast). | `GradingService.Application/Common/Interfaces/IZipProcessingQueue.cs`, `Worker/Worker.cs` |
| **4. gRPC Service** | High-performance calculation microservice utilizing HTTP/2 and Protocol Buffers for dynamic score computation. | `ScoreCalculatorService/Protos/grade_calculator.proto`, `GradingCalcService.cs` |
| **5. Containerization** | Multi-container Docker Compose deployment managing 7 independent containers, shared volumes, and private networks. | `be/docker-compose.yml` |
| **6. Clean Architecture** | Strict separation of concerns: Domain $\rightarrow$ Application (CQRS with MediatR) $\rightarrow$ Infrastructure $\rightarrow$ Presentation API. | `FptuGradingSystem.GradingService/` |