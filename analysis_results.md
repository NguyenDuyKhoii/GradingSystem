# TÀI LIỆU PHÂN TÍCH NGHIỆP VỤ & PHÂN CHIA NHIỆM VỤ THEO ACTOR
## Đề tài 5: Công cụ hỗ trợ chấm bài thi thực hành (PE Grading Tool) - FPTU PE Grading System

---

## 1. Xác nhận Yêu cầu & Cấu hình Hệ thống (Confirmed Requirements)
* **Message Broker:** Sử dụng **Redis (Streams / PubSub)** làm kênh truyền thông điệp bất đồng bộ giữa REST API và Background Worker để giải phóng API khỏi việc xử lý file ZIP nặng.
* **Luồng Tải lên (ZIP Upload Flow):** **Academic Staff (Khảo thí)** là người upload file ZIP bài thi của lớp. Giảng viên (`Lecturer`) chỉ tập trung vào nghiệp vụ xem bài, chấm điểm qua giao diện Split-Screen và xuất Excel.
* **Giao diện:** Chuyển phần card Upload ZIP từ màn hình của Giảng viên ([LecturerDashboard.jsx](file:///d:/PRN_AS/fe/src/components/LecturerDashboard.jsx)) sang màn hình của Khảo thí ([AcademicDashboard.jsx](file:///d:/PRN_AS/fe/src/components/AcademicDashboard.jsx)).

---

## 2. Quy trình Nghiệp vụ Tổng quát (End-to-End Business Flow)

```
[Academic Staff] ──(Tạo Lớp thi & Gán Giảng viên)──> [Hệ thống]
       │
       └──(Upload ZIP bài thi của Lớp)──> [REST API] ──(Gửi Sự kiện qua Redis)──> [Background Worker]
                                                                                            │ (Giải nén & Phân tích tên file)
                                                                                            ▼
[Lecturer] <──(Mở Màn hình Chấm: Xem bài sinh viên qua gRPC) <──(Đọc bài nộp trong DB) ◄───┘
   │
   ├──(Nhập điểm chi tiết + Nhận xét từng tiêu chí của Rubric)
   ├──(Lưu nháp / Công bố điểm)
   └──(Xuất Excel điểm gửi phòng khảo thí)
```

---

## 3. Phân chia Nhiệm vụ chi tiết theo Actor (Task Breakdown by Actor)

Dưới đây là bảng phân chia công việc chi tiết chia theo 3 nhóm tác nhân chính: **Academic Staff (Khảo thí)**, **Lecturer (Giảng viên)**, và **System / Background (Hệ thống tự động)**.

### ACTOR 1: ACADEMIC STAFF (Khảo thí)
Nhóm tính năng dành cho nhân viên khảo thí quản trị hệ thống, thiết lập lớp học, barem điểm và chuẩn bị dữ liệu thi.

| ID | Tên Nhiệm vụ | Mô tả Chi tiết | Thành phần Cần sửa/Tạo |
|---|---|---|---|
| **AST-1** | **Quản lý Môn học (Subjects)** | Thực hiện CRUD các môn học trong trường (ví dụ: PRN232, OSG202...). | `SubjectsController.cs`, `AcademicDashboard.jsx` (Tab Subjects) |
| **AST-2** | **Quản lý Barem điểm (Rubric & Criteria)** | - Thiết lập Rubric cho môn học.<br>- Thêm các tiêu chí chấm (`RubricCriteria`): Tên tiêu chí, Mô tả tiêu chí, Điểm tối đa (`MaxPoints`), Trọng số (`Weight`).<br>- **Ràng buộc nghiệp vụ:** Hệ thống kiểm tra tổng trọng số (`TotalWeight`) của các tiêu chí phải bằng 100% (hoặc 1.0) trước khi cho phép lưu. | `RubricsController.cs`, `AcademicDashboard.jsx` (Tab Rubrics) |
| **AST-3** | **Quản lý Lớp thi & Phân công chấm (Exam Classes)** | Khởi tạo lớp thi (ví dụ: `SE1801`), chọn môn học, học kỳ (ví dụ: `SU26`), và gán Giảng viên chấm thi (`LecturerId`). | `ExamClassesController.cs`, `AcademicDashboard.jsx` (Tab Classes) |
| **AST-4** | **Tải lên bài thi của lớp (Upload ZIP)** | - Khảo thí tải lên file `.zip` chứa các file bài nộp của sinh viên cho lớp thi cụ thể.<br>- Giao diện hiển thị trạng thái của file ZIP (ví dụ: "Đang giải nén...", "Đã giải nén thành công", "Lỗi giải nén"). | **[NEW]** Endpoint `POST /api/ExamClasses/{id}/upload-zip` trong API.<br>**[MODIFY]** Chuyển component Upload ZIP từ `LecturerDashboard.jsx` sang `AcademicDashboard.jsx` (Tab Classes). |

---

### ACTOR 2: LECTURER (Giảng viên)
Nhóm tính năng dành cho giảng viên thực hiện công tác chấm điểm và nhận xét sinh viên.

| ID | Tên Nhiệm vụ | Mô tả Chi tiết | Thành phần Cần sửa/Tạo |
|---|---|---|---|
| **LEC-1** | **Xem danh sách lớp được phân công** | Giảng viên đăng nhập chỉ thấy các lớp thi mà mình được phân công chấm điểm. Hiển thị tiến độ chấm (ví dụ: "Đã chấm 15/30 bài"). | `LecturerDashboard.jsx` (Phần danh sách lớp) |
| **LEC-2** | **Xem danh sách bài nộp của lớp** | Khi chọn lớp thi, giảng viên xem được danh sách sinh viên: Mã sinh viên, Họ tên, Trạng thái bài nộp (`Unassigned` - Chưa chấm, `Draft` - Đã chấm nháp, `Graded` - Đã chấm xong). Hỗ trợ tìm kiếm theo MSSV/Tên và lọc theo trạng thái. | `SubmissionsController.cs`, `LecturerDashboard.jsx` (Bảng sinh viên) |
| **LEC-3** | **Chấm điểm Split-Screen trực quan** | Khi click "Grade" một sinh viên:<br>- **Bên trái:** Trình đọc file hiển thị nội dung thực tế của file bài làm sinh viên (được trích xuất dạng văn bản sạch).<br>- **Bên phải:** Bảng rubric chấm điểm chứa danh sách các tiêu chí.<br>- Giảng viên click chọn mức điểm (0 đến MaxPoints) và nhập nhận xét (Feedback) cho từng tiêu chí.<br>- Nhập nhận xét chung (General Feedback).<br>- Điểm số tổng kết tự động tính toán thời gian thực theo công thức: `Tổng điểm = Sum(Điểm tiêu chí * Trọng số)`. | `GradesController.cs`, [GradingView.jsx](file:///d:/PRN_AS/fe/src/components/GradingView.jsx) |
| **LEC-4** | **Quản lý Trạng thái Điểm (Lưu nháp / Công bố)** | - **Lưu nháp (Save Draft):** Lưu điểm lại để sửa sau, sinh viên chưa xem được điểm.<br>- **Công bố (Submit & Publish):** Điểm được khóa lại và công bố chính thức. | `GradeSubmissionCommand.cs`, `GradingView.jsx` |
| **LEC-5** | **Xuất file Excel điểm của Lớp** | Giảng viên có thể xuất bảng điểm của lớp thi ra định dạng Excel (MSSV, Họ tên, Điểm chi tiết từng tiêu chí, Điểm tổng kết, Nhận xét chung) để phục vụ lưu trữ hoặc nộp báo cáo. | **[NEW]** Endpoint `GET /api/ExamClasses/{id}/export-excel` trong API. |

---

### ACTOR 3: SYSTEM / BACKGROUND (Hệ thống Xử lý Tự động)
Các tác vụ chạy ngầm, giao tiếp dịch vụ và tự động hóa xử lý dữ liệu.

| ID | Tên Nhiệm vụ | Mô tả Chi tiết | Thành phần Cần sửa/Tạo |
|---|---|---|---|
| **SYS-1** | **Đăng tin sự kiện ZIP qua Redis (Broker)** | Khi Khảo thí upload file ZIP, REST API lưu file tạm thời vào thư mục `wwwroot/uploads/temp/` và đẩy một thông điệp chứa `ExamClassId` và `ZipFilePath` vào Redis Stream (hoặc Redis Channel). | `ExamClassesController.cs` (logic upload), Cấu hình Redis Client trong API. |
| **SYS-2** | **Xử lý giải nén bất đồng bộ (Worker)** | Background Worker lắng nghe Redis Stream:<br>1. Nhận sự kiện, thực hiện giải nén file ZIP vào thư mục lưu trữ thực tế (`wwwroot/uploads/submissions/{ExamClassId}/`).<br>2. Quét toàn bộ file trong thư mục vừa giải nén.<br>3. Sử dụng Regex phân tích tên file sinh viên để lấy MSSV và Họ Tên (ví dụ: `SE150123_NguyenVanA.docx` -> MSSV: `SE150123`, Họ Tên: `Nguyen Van A`, Định dạng: `docx`).<br>4. Tạo các bản ghi `Submission` tương ứng vào database với đường dẫn file vật lý tương ứng. | `FptuGradingSystem.Worker` (phát triển lớp `Worker.cs` và các Service đi kèm). |
| **SYS-3** | **Trích xuất nội dung file qua gRPC** | - Phát triển một dịch vụ gRPC độc lập làm nhiệm vụ đọc nội dung file.<br>- Khi có yêu cầu, đọc file `.docx`/`.xlsx`/`.txt` trên server, dùng thư viện OpenXML để trích xuất toàn bộ văn bản thô (Paragraphs) hoặc dữ liệu ô của file.<br>- Trả kết quả văn bản thô qua gRPC response về REST API.<br>- REST API trả về cho React Frontend hiển thị trên màn hình chấm điểm của Giảng viên. | **[NEW]** Thiết lập file proto `document_reader.proto` trong `FptuGradingSystem.GrpcService`. <br>Tích hợp gRPC client trong REST API. |
| **SYS-4** | **Đồng bộ trạng thái Lớp thi tự động** | Hệ thống tự động chuyển trạng thái của `ExamClass`:<br>- `Grading`: Khi có ít nhất 1 bài thi được chấm.<br>- `Completed`: Khi 100% bài thi của lớp đã được chấm chính thức (`Graded`). | `GradeSubmissionCommand.cs` |

---

## 4. Lộ trình thực hiện tiếp theo (Next Action Steps)

Để hiện thực hóa sơ đồ trên, đây là kế hoạch làm việc sắp tới:
1. **Thiết lập hạ tầng Redis:** Chạy Redis qua Docker hoặc cài đặt cục bộ phục vụ làm Broker.
2. **Triển khai luồng upload của Khảo thí (AST-4 & SYS-1 & SYS-2):**
   * Di chuyển phần giao diện upload sang `AcademicDashboard.jsx`.
   * Viết API upload ZIP và đẩy tin nhắn vào Redis ở REST API.
   * Viết Worker xử lý giải nén, phân tích tên file sinh viên và lưu vào Database.
3. **Triển khai dịch vụ gRPC (SYS-3 & LEC-3):**
   * Viết gRPC service trong dự án `FptuGradingSystem.GrpcService` để parse file `.docx`.
   * Gọi gRPC từ REST API để hiển thị văn bản thực tế trong `GradingView.jsx`.
4. **Hoàn thiện API và giao diện chấm bài của Giảng viên (LEC-3 & LEC-4).**
5. **Tính năng phụ trợ:** Xuất báo cáo Excel (LEC-5) và Container hóa Docker compose.
