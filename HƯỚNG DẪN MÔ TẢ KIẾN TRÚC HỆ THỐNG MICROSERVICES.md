# **HƯỚNG DẪN MÔ TẢ KIẾN TRÚC HỆ THỐNG MICROSERVICES** 

## **1. System Context View** 

### **Mục tiêu** 

Mô tả phạm vi của hệ thống và các tác nhân tương tác. 

### **Câu hỏi cần trả lời** 

- Hệ thống phục vụ những đối tượng nào? 

- Có những hệ thống bên ngoài nào kết nối? 

- Người dùng truy cập hệ thống thông qua thành phần nào? 

- Phạm vi của hệ thống gồm những thành phần nào? 

### **Minh chứng** 

- Context Diagram 

- Danh sách Actors 

- Danh sách External Systems 

- Use Case Diagram (nếu có) 

## **2. Runtime View** 

### **Mục tiêu** 

Vẽ diagram Mô tả cách các dịch vụ hoạt động và giao tiếp khi hệ thống đang chạy. 

### **Câu hỏi cần trả lời** 

- Hệ thống có bao nhiêu Services? 

- Chức năng chính của từng Service là gì? 

- Service nào giao tiếp với Service nào? 

- Hình thức giao tiếp là REST API, gRPC hay Message Queue? 

- Service nào là Background Worker? 

- Có Event Publisher/Consumer không? 

- Luồng xử lý của một nghiệp vụ chính diễn ra như thế nào? 

- API Contract của từng Service được định nghĩa ra sao? 

### **Minh chứng** 

- Service Catalog 

- Runtime Architecture Diagram 

- Communication Matrix 

- Sequence Diagram (ít nhất 02 nghiệp vụ) 

- Swagger/OpenAPI của từng Service 

- gRPC .proto (nếu có) 

- Event Flow Diagram (nếu có) 

- Danh sách Background Workers 

## **3. Deployment View** 

### **Mục tiêu** 

Vẽ Diagram mô tả cách hệ thống được triển khai trên hạ tầng. 

### **Câu hỏi cần trả lời** 

- Có bao nhiêu Containers? 

- Mỗi Container chạy Service nào? 

- Các Containers được kết nối bằng Docker Network nào? 

- Port Mapping được cấu hình như thế nào? 

- Database, Redis, RabbitMQ được triển khai ở đâu? 

- Docker Volumes được sử dụng để lưu trữ dữ liệu nào? 

- Toàn bộ hệ thống được khởi động bằng Docker Compose hay Kubernetes? 

### **Minh chứng** 

- Deployment Diagram 

- Docker Compose Architecture 

- File docker-compose.yml 

- Port Mapping 

- Docker Networks 

- Docker Volumes 

- Ảnh chụp docker compose ps hoặc Docker Desktop sau khi triển khai 

## **Yêu cầu chung** 

Mỗi nội dung phải đảm bảo: 

- **Mô tả (Description):** Giải thích kiến trúc hoặc thành phần. 

- **Sơ đồ (Diagram):** Minh họa trực quan. 

- **Minh chứng (Evidence):** Cấu hình, mã nguồn, ảnh chụp màn hình hoặc tài liệu sinh ra từ hệ thống. 

**Lưu ý:** Chỉ chấp nhận các sơ đồ có thể đối chiếu với mã nguồn và cấu hình triển khai thực tế. Sơ đồ không khớp với hệ thống hoặc không có minh chứng sẽ không được tính điểm. 

