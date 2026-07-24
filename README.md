# 🎓 FPTU PE Grading Microservices System

Hệ thống quản lý và chấm điểm thi thực hành (PE) cho FPT University theo kiến trúc **Microservices** tiên tiến (.NET 8, gRPC, Redis Stream/PubSub, PostgreSQL, YARP Gateway và React Vite).

---

## 🛠️ Kiến trúc Hệ thống (System Architecture)

- **YARP API Gateway** (`http://localhost:5000`): Cổng giao tiếp tập trung cho tất cả request từ FE.
- **AuthService** (`http://localhost:5002` / gRPC `:5003`): Quản lý đăng nhập, cấp JWT Token và gRPC sync User.
- **GradingService** (`http://localhost:5004`): Quản lý Lớp thi, Môn học, Barem/Rubric và Bài nộp.
- **ScoreCalculatorService** (gRPC `:5001`): Trích xuất nội dung file (.txt, .cs, .java, .pdf, .docx) và engine tính điểm quy đổi.
- **GradingWorker**: Xử lý giải nén file ZIP bài nộp bất đồng bộ qua Redis Stream & bắn sự kiện Pub/Sub.
- **PostgreSQL & Redis**: Lưu trữ dữ liệu quan hệ và hệ thống Message Broker / Caching.
- **Frontend (React + Vite)** (`http://localhost:5173`): Giao diện Giảng viên & Academic Staff.

---

## 🚀 Hướng Dẫn Chạy Dự Án Cho Người Mới (Quick Start Guide)

### 📋 Yêu cầu tiên quyết (Prerequisites)
1. **Docker Desktop** (Đã bật và đang chạy).
2. **Node.js** (Phiên bản 18+).
3. **Git**.

---

### 🔹 Bước 1: Clone dự án về máy
```bash
git clone https://github.com/NguyenDuyKhoii/GradingSystem.git
cd GradingSystem
```

---

### 🔹 Bước 2: Khởi chạy toàn bộ Backend bằng Docker Compose
Mở Terminal tại thư mục `be`:
```bash
cd be
docker compose up -d --build
```
> ⏱️ *Lần chạy đầu tiên Docker sẽ kéo image và build khoảng 1-2 phút. Sau khi xong, tất cả 7 container (Postgres, Redis, Gateway, Auth, Grading, ScoreCalculator, Worker) sẽ sẵn sàng!*

---

### 🔹 Bước 3: Khởi chạy Frontend (React Vite)
Mở một cửa sổ Terminal mới tại thư mục `fe`:
```bash
cd fe
npm install
npm run dev
```

---

### 🔑 Bước 4: Truy cập ứng dụng & Tài khoản mẫu

Mở trình duyệt truy cập: **`http://localhost:5173`**

| Vai trò (Role) | Tên tài khoản (Username) | Mật khẩu (Password) | Chức năng chính |
| :--- | :--- | :--- | :--- |
| **Academic Staff** | `academic` | `123456` | Tạo môn học, Lớp thi, Khởi tạo Rubric/Barem, Upload file ZIP bài nộp |
| **Lecturer** | `lec1`, `lec2`, `lec3`, `lec5` | `123456` | Xem danh sách lớp được phân công, xem trực tiếp bài nộp và chấm điểm |

---

## 🔍 Kiểm tra Log Hệ Thống (Monitoring Logs)

Để xem các Microservice và Worker làm việc real-time trong terminal:

```bash
# Xem Worker giải nén ZIP & in báo cáo summary:
docker logs -f grading-worker

# Xem gRPC đọc file & tính điểm:
docker logs -f grading-score-calculator-service

# Xem Main API:
docker logs -f grading-api
```
